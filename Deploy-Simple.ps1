#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Property Management System - Simple Kubernetes Deployment Script

.PARAMETER Action
    The action to perform: Clean, Deploy, or All

.PARAMETER ContainerRuntime
    The container runtime to use: Docker or Podman

.EXAMPLE
    .\Deploy-Simple.ps1 -Action All
    .\Deploy-Simple.ps1 -Action Clean
    .\Deploy-Simple.ps1 -Action Deploy
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Clean", "Deploy", "All")]
    [string]$Action = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Docker", "Podman")]  
    [string]$ContainerRuntime = "Docker"
)

# Configuration
$ImageName = "zamahele/property-management"
$ImageTag = "latest"
$Namespace = "property-management"
$KubernetesDir = "kubernetes"

function Write-Step {
    param([string]$Message)
    Write-Host "🔵 $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️ $Message" -ForegroundColor Cyan
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    
    # Check kubectl
    try {
        kubectl version --client --output=yaml 2>$null | Out-Null
        Write-Success "kubectl is available"
    }
    catch {
        Write-ErrorMsg "kubectl is not available"
        return $false
    }
    
    # Check container runtime
    if ($ContainerRuntime -eq "Docker") {
        try {
            docker version 2>$null | Out-Null
            Write-Success "Docker is available"
        }
        catch {
            Write-ErrorMsg "Docker is not available"
            return $false
        }
    }
    else {
        try {
            podman version 2>$null | Out-Null
            Write-Success "Podman is available"
        }
        catch {
            Write-ErrorMsg "Podman is not available"
            return $false
        }
    }
    
    # Check Kubernetes connectivity
    try {
        kubectl cluster-info 2>$null | Out-Null
        Write-Success "Kubernetes cluster is accessible"
    }
    catch {
        Write-ErrorMsg "Cannot connect to Kubernetes cluster"
        return $false
    }
    
    return $true
}

function Invoke-Clean {
    Write-Step "🧹 Cleaning existing deployment..."
    
    try {
        $namespaceExists = kubectl get namespace $Namespace 2>$null
        
        if ($namespaceExists) {
            Write-Info "Deleting existing deployment..."
            kubectl delete namespace $Namespace --ignore-not-found=true
            
            Write-Info "Waiting for cleanup to complete..."
            do {
                Start-Sleep -Seconds 2
                $namespaceStatus = kubectl get namespace $Namespace 2>$null
            } while ($namespaceStatus)
            
            Write-Success "Cleanup completed"
        }
        else {
            Write-Info "No existing deployment found"
        }
        
        return $true
    }
    catch {
        Write-ErrorMsg "Error during cleanup: $($_.Exception.Message)"
        return $false
    }
}

function Invoke-Build {
    Write-Step "🏗️ Building container image..."
    
    try {
        if ($ContainerRuntime -eq "Docker") {
            docker build -t "${ImageName}:${ImageTag}" -f "PropertyManagement.Web/Dockerfile" .
        }
        else {
            podman build -t "${ImageName}:${ImageTag}" -f "PropertyManagement.Web/Dockerfile" .
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Container image built successfully"
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
        kubectl apply -f $KubernetesDir
        
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Failed to apply Kubernetes manifests"
            return $false
        }
        
        Write-Success "Kubernetes manifests applied successfully"
        
        Write-Step "Waiting for SQL Server to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "SQL Server is ready"
        }
        else {
            Write-Warning "SQL Server readiness check timed out"
        }
        
        Write-Step "Waiting for web application to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Namespace
        
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
    
    Write-Info "Deployment Status:"
    kubectl get all -n $Namespace
    Write-Host ""
    
    Write-Info "Pods Status:"
    kubectl get pods -n $Namespace -o wide
    Write-Host ""
    
    Write-Success "🌐 Access Information:"
    Write-Host "Local Access (Port Forward):" -ForegroundColor Cyan
    Write-Host "  kubectl port-forward svc/property-management-service 8080:80 -n $Namespace" -ForegroundColor White
    Write-Host "  Then visit: http://localhost:8080" -ForegroundColor White
    Write-Host ""
    Write-Host "Default Credentials:" -ForegroundColor Cyan
    Write-Host "  Username: Admin" -ForegroundColor White
    Write-Host "  Password: 01Pa" -NoNewline -ForegroundColor White
    Write-Host "$$" -NoNewline -ForegroundColor White
    Write-Host "w0rd2025#" -ForegroundColor White
    Write-Host ""
}

# Main execution
Clear-Host
Write-Host ""
Write-Host "🚀 Property Management System - Kubernetes Deployment" -ForegroundColor Blue
Write-Host "=====================================================" -ForegroundColor Blue
Write-Host ""
Write-Host "Action: $Action" -ForegroundColor Cyan
Write-Host "Container Runtime: $ContainerRuntime" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Prerequisites)) {
    Write-ErrorMsg "Prerequisites check failed"
    exit 1
}

try {
    switch ($Action) {
        "Clean" {
            if (-not (Invoke-Clean)) {
                exit 1
            }
        }
        "Deploy" {
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
        Write-Host "kubectl port-forward svc/property-management-service 8080:80 -n $Namespace" -ForegroundColor White
    }
}
catch {
    Write-ErrorMsg "An unexpected error occurred: $($_.Exception.Message)"
    exit 1
}