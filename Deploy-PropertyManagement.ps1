#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Property Management System - Clean, Configure & Deploy Script for Kubernetes

.DESCRIPTION
    This script provides a comprehensive deployment solution for the Property Management System.
    It can clean existing deployments, configure the system, and deploy to Kubernetes.

.PARAMETER Action
    The action to perform: Clean, Configure, Deploy, or All

.PARAMETER ContainerRuntime
    The container runtime to use: Docker or Podman

.PARAMETER Domain
    The domain name for ingress configuration

.PARAMETER SkipBuild
    Skip the container image build step

.EXAMPLE
    .\Deploy-PropertyManagement.ps1 -Action All
    .\Deploy-PropertyManagement.ps1 -Action Clean
    .\Deploy-PropertyManagement.ps1 -Action Deploy -SkipBuild
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Clean", "Configure", "Deploy", "All")]
    [string]$Action = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Docker", "Podman")]
    [string]$ContainerRuntime = "Docker",
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = "property-management.zamahele.com",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

# Configuration
$Script:Config = @{
    ImageName = "zamahele/property-management"
    ImageTag = "latest"
    Namespace = "property-management"
    KubernetesDir = "kubernetes"
    DockerfilePath = "PropertyManagement.Web/Dockerfile"
    DefaultCredentials = @{
        Username = "Admin"
        Password = "01Pa$$w0rd2025#"
    }
}

# Colors for output
$Script:Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Blue"
    Cyan = "Cyan"
    Magenta = "Magenta"
}

function Write-Step {
    param([string]$Message)
    Write-Host "🔵 $Message" -ForegroundColor $Script:Colors.Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Script:Colors.Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor $Script:Colors.Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Script:Colors.Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️ $Message" -ForegroundColor $Script:Colors.Cyan
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    
    $prerequisites = @()
    
    # Check kubectl
    try {
        $null = kubectl version --client --output=yaml 2>$null
        Write-Success "kubectl is available"
    }
    catch {
        $prerequisites += "kubectl"
    }
    
    # Check container runtime
    if ($ContainerRuntime -eq "Docker") {
        try {
            $null = docker version 2>$null
            Write-Success "Docker is available"
        }
        catch {
            $prerequisites += "Docker"
        }
    }
    else {
        try {
            $null = podman version 2>$null
            Write-Success "Podman is available"
        }
        catch {
            $prerequisites += "Podman"
        }
    }
    
    # Check Kubernetes connectivity
    try {
        $null = kubectl cluster-info 2>$null
        Write-Success "Kubernetes cluster is accessible"
    }
    catch {
        $prerequisites += "Kubernetes cluster connection"
    }
    
    if ($prerequisites.Count -gt 0) {
        Write-ErrorMsg "Missing prerequisites: $($prerequisites -join ', ')"
        return $false
    }
    
    Write-Success "All prerequisites met"
    return $true
}

function Invoke-Clean {
    Write-Step "🧹 Cleaning existing deployment..."
    
    try {
        # Check if namespace exists
        $namespaceExists = kubectl get namespace $Script:Config.Namespace 2>$null
        
        if ($namespaceExists) {
            Write-Info "Deleting existing deployment in namespace '$($Script:Config.Namespace)'..."
            
            # Delete ingress first to release load balancer
            kubectl delete ingress --all -n $Script:Config.Namespace --ignore-not-found=true
            
            # Delete services
            kubectl delete services --all -n $Script:Config.Namespace --ignore-not-found=true
            
            # Delete deployments
            kubectl delete deployments --all -n $Script:Config.Namespace --ignore-not-found=true
            
            # Delete PVCs (this will also delete PVs)
            kubectl delete pvc --all -n $Script:Config.Namespace --ignore-not-found=true
            
            # Delete configmaps and secrets
            kubectl delete configmaps --all -n $Script:Config.Namespace --ignore-not-found=true
            kubectl delete secrets --all -n $Script:Config.Namespace --ignore-not-found=true
            
            # Delete namespace
            kubectl delete namespace $Script:Config.Namespace --ignore-not-found=true
            
            # Wait for namespace deletion
            Write-Info "Waiting for namespace deletion to complete..."
            do {
                Start-Sleep -Seconds 2
                $namespaceStatus = kubectl get namespace $Script:Config.Namespace 2>$null
            } while ($namespaceStatus)
            
            Write-Success "Cleanup completed"
        }
        else {
            Write-Info "No existing deployment found"
        }
        
        # Clean up local images if requested
        $response = Read-Host "Do you want to remove local container images? (y/N)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            try {
                if ($ContainerRuntime -eq "Docker") {
                    docker rmi "$($Script:Config.ImageName):$($Script:Config.ImageTag)" 2>$null
                    docker rmi "localhost/$($Script:Config.ImageName):$($Script:Config.ImageTag)" 2>$null
                }
                else {
                    podman rmi "$($Script:Config.ImageName):$($Script:Config.ImageTag)" 2>$null
                    podman rmi "localhost/$($Script:Config.ImageName):$($Script:Config.ImageTag)" 2>$null
                }
                Write-Success "Local images cleaned"
            }
            catch {
                Write-Warning "Some images could not be removed (may not exist)"
            }
        }
    }
    catch {
        Write-ErrorMsg "Error during cleanup: $($_.Exception.Message)"
        return $false
    }
    
    return $true
}

function Invoke-Configure {
    Write-Step "⚙️ Configuring deployment..."
    
    try {
        # Update domain in ingress if different from default
        $ingressFile = Join-Path $Script:Config.KubernetesDir "08-ingress.yaml"
        if (Test-Path $ingressFile) {
            $ingressContent = Get-Content $ingressFile -Raw
            $ingressContent = $ingressContent -replace "property-management\.zamahele\.com", $Domain
            Set-Content -Path $ingressFile -Value $ingressContent
            Write-Success "Updated ingress domain to: $Domain"
        }
        
        # Verify all required files exist
        $requiredFiles = @(
            "01-namespace.yaml",
            "02-secrets.yaml", 
            "03-configmap.yaml",
            "04-persistent-volume.yaml",
            "05-sql-server.yaml",
            "06-web-app.yaml",
            "07-services.yaml",
            "08-ingress.yaml"
        )
        
        $missingFiles = @()
        foreach ($file in $requiredFiles) {
            $filePath = Join-Path $Script:Config.KubernetesDir $file
            if (-not (Test-Path $filePath)) {
                $missingFiles += $file
            }
        }
        
        if ($missingFiles.Count -gt 0) {
            Write-ErrorMsg "Missing required Kubernetes manifest files: $($missingFiles -join ', ')"
            return $false
        }
        
        Write-Success "Configuration completed"
        return $true
    }
    catch {
        Write-ErrorMsg "Error during configuration: $($_.Exception.Message)"
        return $false
    }
}

function Invoke-Build {
    if ($SkipBuild) {
        Write-Info "Skipping container image build as requested"
        return $true
    }
    
    Write-Step "🏗️ Building container image..."
    
    try {
        $buildCommand = if ($ContainerRuntime -eq "Docker") {
            "docker build -t `"$($Script:Config.ImageName):$($Script:Config.ImageTag)`" -f `"$($Script:Config.DockerfilePath)`" ."
        }
        else {
            "podman build -t `"$($Script:Config.ImageName):$($Script:Config.ImageTag)`" -f `"$($Script:Config.DockerfilePath)`" ."
        }
        
        Write-Info "Executing: $buildCommand"
        Invoke-Expression $buildCommand
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Container image built successfully: $($Script:Config.ImageName):$($Script:Config.ImageTag)"
            return $true
        }
        else {
            Write-ErrorMsg "Container image build failed"
            return $false
        }
    }
    catch {
        Write-ErrorMsg "Error during build: $($_.Exception.Message)"
        return $false
    }
}

function Invoke-Deploy {
    Write-Step "🚀 Deploying to Kubernetes..."
    
    try {
        # Apply Kubernetes manifests
        Write-Info "Applying Kubernetes manifests..."
        kubectl apply -f $Script:Config.KubernetesDir
        
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Failed to apply Kubernetes manifests"
            return $false
        }
        
        Write-Success "Kubernetes manifests applied successfully"
        
        # Wait for SQL Server to be ready
        Write-Step "Waiting for SQL Server to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Script:Config.Namespace
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "SQL Server is ready"
        }
        else {
            Write-Warning "SQL Server readiness check timed out, but continuing..."
        }
        
        # Wait for web application to be ready
        Write-Step "Waiting for web application to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Script:Config.Namespace
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Web application is ready"
        }
        else {
            Write-Warning "Web application readiness check timed out"
        }
        
        return $true
    }
    catch {
        Write-ErrorMsg "Error during deployment: $($_.Exception.Message)"
        return $false
    }
}

function Show-DeploymentInfo {
    Write-Step "📊 Deployment Information"
    Write-Host ""
    
    # Show deployment status
    Write-Info "Deployment Status:"
    kubectl get all -n $Script:Config.Namespace
    Write-Host ""
    
    # Show pods in detail
    Write-Info "Pods Status:"
    kubectl get pods -n $Script:Config.Namespace -o wide
    Write-Host ""
    
    # Show services
    Write-Info "Services:"
    kubectl get svc -n $Script:Config.Namespace
    Write-Host ""
    
    # Show ingress
    Write-Info "Ingress:"
    kubectl get ingress -n $Script:Config.Namespace
    Write-Host ""
    
    # Access information
    Write-Success "🌐 Access Information:"
    Write-Host "===================="
    Write-Host "Local Access (Port Forward):" -ForegroundColor $Script:Colors.Cyan
    Write-Host "  kubectl port-forward svc/property-management-service 8080:80 -n $($Script:Config.Namespace)" -ForegroundColor White
    Write-Host "  Then visit: http://localhost:8080" -ForegroundColor White
    Write-Host ""
    Write-Host "External Access:" -ForegroundColor $Script:Colors.Cyan
    Write-Host "  https://$Domain" -ForegroundColor White
    Write-Host ""
    Write-Host "Default Credentials:" -ForegroundColor $Script:Colors.Cyan
    Write-Host "  Username: $($Script:Config.DefaultCredentials.Username)" -ForegroundColor White
    Write-Host "  Password: $($Script:Config.DefaultCredentials.Password)" -ForegroundColor White
    Write-Host ""
    Write-Host "Monitoring:" -ForegroundColor $Script:Colors.Cyan
    Write-Host "  Metrics: https://$Domain/metrics" -ForegroundColor White
    Write-Host "  Health: https://$Domain/health" -ForegroundColor White
    Write-Host ""
    
    # Show logs
    Write-Info "Recent Application Logs:"
    kubectl logs -l app=property-management-web -n $Script:Config.Namespace --tail=10 2>$null
}

# Main execution
function Main {
    Clear-Host
    Write-Host ""
    Write-Host "🚀 Property Management System - Kubernetes Deployment" -ForegroundColor $Script:Colors.Blue
    Write-Host "=====================================================" -ForegroundColor $Script:Colors.Blue
    Write-Host ""
    Write-Host "Action: $Action" -ForegroundColor $Script:Colors.Cyan
    Write-Host "Container Runtime: $ContainerRuntime" -ForegroundColor $Script:Colors.Cyan
    Write-Host "Domain: $Domain" -ForegroundColor $Script:Colors.Cyan
    Write-Host "Skip Build: $SkipBuild" -ForegroundColor $Script:Colors.Cyan
    Write-Host ""
    
    # Check prerequisites
    if (-not (Test-Prerequisites)) {
        Write-ErrorMsg "Prerequisites check failed. Please install missing components."
        exit 1
    }
    
    try {
        switch ($Action) {
            "Clean" {
                if (-not (Invoke-Clean)) {
                    exit 1
                }
            }
            "Configure" {
                if (-not (Invoke-Configure)) {
                    exit 1
                }
            }
            "Deploy" {
                if (-not (Invoke-Configure)) {
                    exit 1
                }
                if (-not (Invoke-Build)) {
                    exit 1
                }
                if (-not (Invoke-Deploy)) {
                    exit 1
                }
                Show-DeploymentInfo
            }
            "All" {
                if (-not (Invoke-Clean)) {
                    exit 1
                }
                if (-not (Invoke-Configure)) {
                    exit 1
                }
                if (-not (Invoke-Build)) {
                    exit 1
                }
                if (-not (Invoke-Deploy)) {
                    exit 1
                }
                Show-DeploymentInfo
            }
        }
        
        Write-Host ""
        Write-Success "🎉 Operation completed successfully!"
        Write-Host ""
        
        if ($Action -eq "Deploy" -or $Action -eq "All") {
            Write-Info "Quick Access Command:"
            Write-Host "kubectl port-forward svc/property-management-service 8080:80 -n $($Script:Config.Namespace)" -ForegroundColor White
        }
    }
    catch {
        Write-ErrorMsg "An unexpected error occurred: $($_.Exception.Message)"
        exit 1
    }
}

# Execute main function
Main