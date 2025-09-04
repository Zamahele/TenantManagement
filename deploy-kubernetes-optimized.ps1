#!/usr/bin/env pwsh

# Property Management System - Production Kubernetes Deployment Script
# Optimized for database preservation and migrations

param(
    [switch]$CleanDeploy,      # Full clean deploy (destroys everything including database)
    [switch]$ViewLogs,         # View deployment logs
    [switch]$MigrateOnly,      # Only run database migrations
    [switch]$WebOnly,          # Only redeploy web application
    [switch]$SkipMigrations,   # Skip database migrations
    [string]$GitHubEmail = $env:GITHUB_EMAIL,
    [string]$GitHubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
$Namespace = "property-management"

# Function to view logs
function Show-Logs {
    Write-Host "Checking deployment status..." -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    
    Write-Host "`nChecking migration job logs..." -ForegroundColor Cyan
    $migrationPods = kubectl get pods -n $Namespace -l job-name=migration-job -o jsonpath='{.items[*].metadata.name}' 2>$null
    if ($migrationPods) {
        $migrationPods.Split() | ForEach-Object {
            Write-Host "Migration job logs for pod: $_" -ForegroundColor Yellow
            kubectl logs $_ -n $Namespace
        }
    }
    
    Write-Host "`nChecking web app logs..." -ForegroundColor Cyan
    $webPods = kubectl get pods -n $Namespace -l app=property-management-web -o jsonpath='{.items[*].metadata.name}' 2>$null
    if ($webPods) {
        $podName = $webPods.Split()[0]
        Write-Host "Web app logs for pod: $podName" -ForegroundColor Yellow
        kubectl logs $podName -n $Namespace --tail=50
    }
    
    Write-Host "`nSQL Server logs:" -ForegroundColor Yellow
    kubectl logs deployment/sql-server -n $Namespace --tail=20
    
    Write-Host "`nRecent events:" -ForegroundColor Yellow
    kubectl get events -n $Namespace --sort-by=.lastTimestamp --field-selector type!=Normal
}

# Function to create required secrets
function Create-RequiredSecrets {
    Write-Host "Creating/updating required secrets..." -ForegroundColor Cyan
    
    # Create SSL certificate secret if aspnetapp.pfx exists
    if (Test-Path "aspnetapp.pfx") {
        Write-Host "Creating/updating SSL certificate secret..."
        kubectl create secret generic https-cert --from-file=aspnetapp.pfx=aspnetapp.pfx -n $Namespace --dry-run=client -o yaml | kubectl apply -f -
    } else {
        Write-Warning "SSL certificate file 'aspnetapp.pfx' not found in current directory"
        Write-Host "You can generate one with: dotnet dev-certs https -ep aspnetapp.pfx -p YourPassword"
    }
    
    # Create GHCR secret if credentials are provided
    if ($GitHubEmail -and $GitHubToken) {
        Write-Host "Creating/updating GitHub Container Registry secret..."
        kubectl create secret docker-registry ghcr-secret `
            --docker-server=ghcr.io `
            --docker-username=$GitHubEmail `
            --docker-password=$GitHubToken `
            --docker-email=$GitHubEmail `
            -n $Namespace --dry-run=client -o yaml | kubectl apply -f -
    } else {
        Write-Warning "GitHub credentials not provided. Set GITHUB_EMAIL and GITHUB_TOKEN environment variables"
        Write-Host "Or run: `$env:GITHUB_EMAIL='your-email'; `$env:GITHUB_TOKEN='your-token'"
    }
}

# Function to run database migrations
function Run-DatabaseMigrations {
    Write-Host "Running database migrations..." -ForegroundColor Cyan
    
    # Delete any existing migration job
    kubectl delete job migration-job -n $Namespace --ignore-not-found=true
    
    # Wait for job deletion
    Start-Sleep -Seconds 5
    
    # Create migration job YAML
    $migrationJobYaml = @"
apiVersion: batch/v1
kind: Job
metadata:
  name: migration-job
  namespace: $Namespace
spec:
  template:
    spec:
      restartPolicy: Never
      containers:
      - name: migration
        image: ghcr.io/zamahele/tenantmanagement:latest
        command: ["dotnet", "ef", "database", "update", "--project", "/app/PropertyManagement.Infrastructure", "--startup-project", "/app/PropertyManagement.Web"]
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          value: "Server=sql-server.property-management.svc.cluster.local,1433;Database=PropertyManagement;User Id=sa;Password=01Pa`$`$w0rd2025#;TrustServerCertificate=true;"
        - name: DOTNET_ENVIRONMENT
          value: "Production"
      imagePullSecrets:
      - name: ghcr-secret
  backoffLimit: 3
"@
    
    # Apply migration job
    $migrationJobYaml | kubectl apply -f -
    
    # Wait for migration job to complete
    Write-Host "Waiting for database migrations to complete..."
    $jobCompleted = kubectl wait --for=condition=complete --timeout=300s job/migration-job -n $Namespace 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Migration job timeout or failed. Checking status..."
        kubectl describe job/migration-job -n $Namespace
        
        # Show migration logs
        $migrationPods = kubectl get pods -n $Namespace -l job-name=migration-job -o jsonpath='{.items[*].metadata.name}' 2>$null
        if ($migrationPods) {
            $migrationPods.Split() | ForEach-Object {
                Write-Host "Migration logs for pod: $_" -ForegroundColor Yellow
                kubectl logs $_ -n $Namespace
            }
        }
        return $false
    } else {
        Write-Host "Database migrations completed successfully!" -ForegroundColor Green
        return $true
    }
}

# Function to deploy/update web application only
function Deploy-WebApplication {
    Write-Host "Deploying/updating web application..." -ForegroundColor Cyan
    
    # Update the web application deployment
    kubectl apply -f k8s/06-web-app.yaml
    
    # Force a rolling update by updating the deployment annotation
    $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
    kubectl patch deployment property-management-web -n $Namespace -p "{`"spec`":{`"template`":{`"metadata`":{`"annotations`":{`"deployment.kubernetes.io/restart`":`"$timestamp`"}}}}}"
    
    # Wait for web application rollout
    Write-Host "Waiting for web application rollout to complete..."
    $rolloutSuccess = kubectl rollout status deployment/property-management-web -n $Namespace --timeout=300s 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Web application rollout timeout or failed. Checking status..."
        kubectl get pods -n $Namespace -l app=property-management-web
        kubectl describe deployment/property-management-web -n $Namespace
        return $false
    } else {
        Write-Host "Web application updated successfully!" -ForegroundColor Green
        return $true
    }
}

# Function to ensure infrastructure is ready (without destroying database)
function Ensure-Infrastructure {
    Write-Host "Ensuring infrastructure is ready..." -ForegroundColor Cyan
    
    # Apply base resources (these are safe to reapply)
    kubectl apply -f k8s/01-namespace.yaml
    kubectl apply -f k8s/02-secrets.yaml
    kubectl apply -f k8s/03-configmap.yaml
    kubectl apply -f k8s/04-storage.yaml
    
    # Create required secrets
    Create-RequiredSecrets
    
    # Check if SQL Server is already running
    $sqlServerExists = kubectl get deployment sql-server -n $Namespace 2>$null
    if (-not $sqlServerExists) {
        Write-Host "SQL Server not found. Deploying SQL Server..."
        kubectl apply -f k8s/05-sql-server.yaml
        
        # Wait for SQL Server
        Write-Host "Waiting for SQL Server to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace
        Write-Host "SQL Server is ready" -ForegroundColor Green
    } else {
        Write-Host "SQL Server already exists and running" -ForegroundColor Green
    }
    
    # Ensure services exist
    kubectl apply -f k8s/07-services.yaml
    
    # Deploy ingress if available
    if (Test-Path "k8s/08-ingress.yaml") {
        kubectl apply -f k8s/08-ingress.yaml
    }
}

# Main execution starts here
if ($ViewLogs) {
    Show-Logs
    exit 0
}

Write-Host "Property Management System - Production Deployment" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Verify prerequisites
try {
    kubectl version --client | Out-Null
    Write-Host "kubectl found" -ForegroundColor Green
} catch {
    Write-Error "kubectl is required but not found in PATH"
    exit 1
}

try {
    kubectl cluster-info | Out-Null
    Write-Host "Kubernetes cluster accessible" -ForegroundColor Green
} catch {
    Write-Error "Cannot connect to Kubernetes cluster"
    exit 1
}

try {
    # Handle different deployment scenarios
    if ($CleanDeploy) {
        Write-Host "CLEAN DEPLOY: This will destroy EVERYTHING including the database!" -ForegroundColor Red
        $confirmation = Read-Host "Are you sure? Type 'YES' to confirm"
        if ($confirmation -eq "YES") {
            Write-Host "Performing clean deployment..." -ForegroundColor Yellow
            kubectl delete namespace $Namespace --ignore-not-found=true
            
            # Wait for cleanup
            $timeout = 60
            $elapsed = 0
            do {
                Start-Sleep -Seconds 2
                $elapsed += 2
                $namespaceExists = kubectl get namespace $Namespace --ignore-not-found=true 2>$null
                if ($elapsed -gt $timeout) {
                    Write-Warning "Cleanup timeout - proceeding"
                    break
                }
            } while ($namespaceExists)
            
            # Full deployment
            Ensure-Infrastructure
            
            if (-not $SkipMigrations) {
                Run-DatabaseMigrations | Out-Null
            }
            
            Deploy-WebApplication | Out-Null
            
        } else {
            Write-Host "Clean deployment cancelled." -ForegroundColor Yellow
            exit 0
        }
    }
    elseif ($MigrateOnly) {
        Write-Host "Running database migrations only..." -ForegroundColor Cyan
        Ensure-Infrastructure
        $migrationSuccess = Run-DatabaseMigrations
        if (-not $migrationSuccess) {
            Write-Error "Migration failed"
            exit 1
        }
    }
    elseif ($WebOnly) {
        Write-Host "Deploying web application only..." -ForegroundColor Cyan
        $webSuccess = Deploy-WebApplication
        if (-not $webSuccess) {
            Write-Error "Web application deployment failed"
            exit 1
        }
    }
    else {
        # Default: Production-friendly deployment
        Write-Host "Performing production deployment (preserving database)..." -ForegroundColor Cyan
        
        # Ensure infrastructure without destroying database
        Ensure-Infrastructure
        
        # Run migrations unless explicitly skipped
        if (-not $SkipMigrations) {
            Write-Host "Running database migrations..." -ForegroundColor Cyan
            $migrationSuccess = Run-DatabaseMigrations
            if (-not $migrationSuccess) {
                Write-Warning "Migration failed, but continuing with web deployment..."
            }
        } else {
            Write-Host "Skipping database migrations as requested" -ForegroundColor Yellow
        }
        
        # Deploy/update web application
        $webSuccess = Deploy-WebApplication
        if (-not $webSuccess) {
            Write-Error "Web application deployment failed"
            exit 1
        }
    }
    
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    
    # Show final status
    Write-Host "`nDeployment Status:" -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    kubectl get services -n $Namespace
    
    # Show access information
    Write-Host "`nAccess Information:" -ForegroundColor Cyan
    Write-Host "HTTPS: https://localhost:30443" -ForegroundColor Green
    Write-Host "HTTP:  http://localhost:30080" -ForegroundColor Green
    Write-Host "Login - Username: Admin, Password: 01Pa`$`$w0rd2025#"
    
    Write-Host "`nUseful Commands:" -ForegroundColor Yellow
    Write-Host "View logs:         .\deploy-kubernetes-optimized.ps1 -ViewLogs"
    Write-Host "Web only deploy:   .\deploy-kubernetes-optimized.ps1 -WebOnly"
    Write-Host "Migrate only:      .\deploy-kubernetes-optimized.ps1 -MigrateOnly"
    Write-Host "Skip migrations:   .\deploy-kubernetes-optimized.ps1 -SkipMigrations"
    Write-Host "Clean deploy:      .\deploy-kubernetes-optimized.ps1 -CleanDeploy"
    Write-Host "Check status:      kubectl get pods -n $Namespace"
    
} catch {
    Write-Host "Deployment failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host "`nRunning diagnostics..." -ForegroundColor Yellow
    Show-Logs
    
    exit 1
}