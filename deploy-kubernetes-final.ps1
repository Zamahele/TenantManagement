#!/usr/bin/env pwsh

# Property Management System - Kubernetes Deployment Script

param(
    [switch]$CleanDeploy,
    [switch]$ViewLogs,
    [string]$GitHubEmail = $env:GITHUB_EMAIL,
    [string]$GitHubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
$Namespace = "property-management"

# Function to view logs
function Show-Logs {
    Write-Host "Checking deployment status..." -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    
    Write-Host "`nChecking init container logs..." -ForegroundColor Cyan
    $pods = kubectl get pods -n $Namespace -o jsonpath='{.items[*].metadata.name}' | Select-String "property-management-web"
    if ($pods) {
        $podName = $pods.ToString().Split()[0]
        Write-Host "Init container logs for pod: $podName" -ForegroundColor Yellow
        kubectl logs $podName -c wait-for-db -n $Namespace
        
        Write-Host "`nMain container logs (if available):" -ForegroundColor Yellow
        kubectl logs $podName -n $Namespace --container=web-app 2>$null
    }
    
    Write-Host "`nSQL Server logs:" -ForegroundColor Yellow
    kubectl logs deployment/sql-server -n $Namespace --tail=20
    
    Write-Host "`nRecent events:" -ForegroundColor Yellow
    kubectl get events -n $Namespace --sort-by=.lastTimestamp --field-selector type!=Normal
}

# Function to create required secrets
function Create-RequiredSecrets {
    Write-Host "Creating required secrets..." -ForegroundColor Cyan
    
    # Create SSL certificate secret if aspnetapp.pfx exists
    if (Test-Path "aspnetapp.pfx") {
        Write-Host "Creating SSL certificate secret..."
        kubectl create secret generic https-cert --from-file=aspnetapp.pfx=aspnetapp.pfx -n $Namespace --dry-run=client -o yaml | kubectl apply -f -
    } else {
        Write-Warning "SSL certificate file 'aspnetapp.pfx' not found in current directory"
        Write-Host "You can generate one with: dotnet dev-certs https -ep aspnetapp.pfx -p YourPassword"
    }
    
    # Create GHCR secret if credentials are provided
    if ($GitHubEmail -and $GitHubToken) {
        Write-Host "Creating GitHub Container Registry secret..."
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

# If only viewing logs, do that and exit
if ($ViewLogs) {
    Show-Logs
    exit 0
}

Write-Host "Property Management System - Kubernetes Deployment" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Verify kubectl is available
try {
    kubectl version --client | Out-Null
    Write-Host "kubectl found" -ForegroundColor Green
} catch {
    Write-Error "kubectl is required but not found in PATH"
    exit 1
}

# Test cluster connectivity
try {
    kubectl cluster-info | Out-Null
    Write-Host "Kubernetes cluster accessible" -ForegroundColor Green
} catch {
    Write-Error "Cannot connect to Kubernetes cluster"
    exit 1
}

# Clean deployment if requested
if ($CleanDeploy) {
    Write-Host "Cleaning existing deployment..." -ForegroundColor Yellow
    kubectl delete namespace $Namespace --ignore-not-found=true
    
    # Clean up Docker images containing "tenantmanagement"
    Write-Host "Cleaning up Docker images containing 'tenantmanagement'..." -ForegroundColor Yellow
    try {
        # Check if Docker is available
        docker version | Out-Null
        
        # Get images containing "tenantmanagement"
        $tenantImages = docker images --format "table {{.Repository}}:{{.Tag}}" | Select-String -Pattern "tenantmanagement" -AllMatches
        
        if ($tenantImages) {
            Write-Host "Found Docker images containing 'tenantmanagement':" -ForegroundColor Cyan
            $tenantImages | ForEach-Object {
                $imageName = $_.ToString().Trim()
                Write-Host "  - $imageName" -ForegroundColor Gray
            }
            
            # Remove the images
            $tenantImages | ForEach-Object {
                $imageName = $_.ToString().Trim()
                try {
                    Write-Host "Removing image: $imageName" -ForegroundColor Yellow
                    docker rmi $imageName --force 2>$null
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "Successfully removed: $imageName" -ForegroundColor Green
                    }
                } catch {
                    Write-Warning "Failed to remove image: $imageName - $($_.Exception.Message)"
                }
            }
            
            # Clean up dangling images
            Write-Host "Cleaning up dangling images..." -ForegroundColor Yellow
            docker image prune -f | Out-Null
            
        } else {
            Write-Host "No Docker images containing 'tenantmanagement' found" -ForegroundColor Gray
        }
        
    } catch {
        Write-Warning "Docker not available or failed to clean images: $($_.Exception.Message)"
        Write-Host "Continuing with Kubernetes cleanup..." -ForegroundColor Gray
    }
    
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
    Write-Host "Cleanup completed" -ForegroundColor Green
}

try {
    Write-Host "Starting deployment..." -ForegroundColor Cyan
    
    # Deploy base resources
    Write-Host "Creating namespace and base resources..."
    kubectl apply -f k8s/01-namespace.yaml
    kubectl apply -f k8s/02-secrets.yaml
    kubectl apply -f k8s/03-configmap.yaml
    kubectl apply -f k8s/04-storage.yaml
    
    # Create required secrets
    Create-RequiredSecrets
    
    # Deploy SQL Server
    Write-Host "Deploying SQL Server..."
    kubectl apply -f k8s/05-sql-server.yaml
    
    # Wait for SQL Server with better timeout
    Write-Host "Waiting for SQL Server to be ready (this may take several minutes)..."
    $sqlReady = kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "SQL Server deployment timeout or failed. Checking status..."
        kubectl get pods -n $Namespace
        kubectl describe deployment/sql-server -n $Namespace
        Write-Host "SQL Server logs:"
        kubectl logs deployment/sql-server -n $Namespace --tail=20
    } else {
        Write-Host "SQL Server is ready" -ForegroundColor Green
    }
    
    # Verify SQL Server service is accessible
    Write-Host "Verifying SQL Server service..."
    Start-Sleep -Seconds 10
    
    # Deploy web application
    Write-Host "Deploying web application..."
    kubectl apply -f k8s/06-web-app.yaml
    kubectl apply -f k8s/07-services.yaml
    
    # Wait for web application with better error handling
    Write-Host "Waiting for web application to be ready..."
    $webReady = kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Namespace 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Web application deployment timeout. Checking status..."
        Show-Logs
    } else {
        Write-Host "Web application is ready" -ForegroundColor Green
    }
    
    # Deploy ingress if available
    if (Test-Path "k8s/08-ingress.yaml") {
        Write-Host "Creating ingress..."
        kubectl apply -f k8s/08-ingress.yaml
    }
    
    Write-Host "Deployment process completed!" -ForegroundColor Green
    
    # Show status
    Write-Host "`nDeployment Status:" -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    
    # Check if pods are ready
    $notReadyPods = kubectl get pods -n $Namespace --field-selector=status.phase!=Running -o jsonpath='{.items[*].metadata.name}' 2>$null
    if ($notReadyPods) {
        Write-Host "`nSome pods are not ready. Use the following command to troubleshoot:" -ForegroundColor Yellow
        Write-Host ".\deploy-kubernetes-final.ps1 -ViewLogs" -ForegroundColor White
    } else {
        Write-Host "`nAccess Information:" -ForegroundColor Cyan
        Write-Host "HTTPS: https://localhost:30443" -ForegroundColor Green
        Write-Host "HTTP:  http://localhost:30080" -ForegroundColor Green
        Write-Host "Login - Username: Admin, Password: 01Pa`$`$w0rd2025#"
    }
    
    Write-Host "`nUseful Commands:" -ForegroundColor Yellow
    Write-Host "View logs:    .\deploy-kubernetes-final.ps1 -ViewLogs"
    Write-Host "Check status: kubectl get pods -n $Namespace"
    Write-Host "Clean deploy: .\deploy-kubernetes-final.ps1 -CleanDeploy"
    
} catch {
    Write-Host "Deployment failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host "`nRunning diagnostics..." -ForegroundColor Yellow
    Show-Logs
    
    exit 1
}