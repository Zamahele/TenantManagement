param(
    [string]$Action = "All",
    [string]$ContainerRuntime = "Docker"
)

$ImageName = "zamahele/property-management"
$ImageTag = "latest"
$Namespace = "property-management"
$KubernetesDir = "kubernetes"

Clear-Host
Write-Host "Property Management System - Kubernetes Deployment" -ForegroundColor Blue
Write-Host "Action: $Action" -ForegroundColor Cyan
Write-Host "Container Runtime: $ContainerRuntime" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Blue

try {
    kubectl version --client 2>$null | Out-Null
    Write-Host "kubectl: OK" -ForegroundColor Green
}
catch {
    Write-Host "kubectl: FAILED" -ForegroundColor Red
    exit 1
}

try {
    if ($ContainerRuntime -eq "Docker") {
        docker version 2>$null | Out-Null
        Write-Host "Docker: OK" -ForegroundColor Green
    }
    else {
        podman version 2>$null | Out-Null
        Write-Host "Podman: OK" -ForegroundColor Green
    }
}
catch {
    Write-Host "$ContainerRuntime FAILED" -ForegroundColor Red
    exit 1
}

try {
    kubectl cluster-info 2>$null | Out-Null
    Write-Host "Kubernetes: OK" -ForegroundColor Green
}
catch {
    Write-Host "Kubernetes: FAILED" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Clean if requested
if ($Action -eq "Clean" -or $Action -eq "All") {
    Write-Host "Cleaning existing deployment..." -ForegroundColor Blue
    
    $namespaceExists = kubectl get namespace $Namespace 2>$null
    if ($namespaceExists) {
        Write-Host "Deleting namespace..." -ForegroundColor Yellow
        kubectl delete namespace $Namespace --ignore-not-found=true
        
        Write-Host "Waiting for cleanup..." -ForegroundColor Yellow
        do {
            Start-Sleep -Seconds 2
            $namespaceStatus = kubectl get namespace $Namespace 2>$null
        } while ($namespaceStatus)
        
        Write-Host "Cleanup completed" -ForegroundColor Green
    }
    else {
        Write-Host "No existing deployment found" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Build and Deploy if requested
if ($Action -eq "Deploy" -or $Action -eq "All") {
    Write-Host "Building container image..." -ForegroundColor Blue
    
    if ($ContainerRuntime -eq "Docker") {
        docker build -t "${ImageName}:${ImageTag}" -f "PropertyManagement.Web/Dockerfile" .
    }
    else {
        podman build -t "${ImageName}:${ImageTag}" -f "PropertyManagement.Web/Dockerfile" .
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully" -ForegroundColor Green
    }
    else {
        Write-Host "Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "Deploying to Kubernetes..." -ForegroundColor Blue
    
    kubectl apply -f $KubernetesDir
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Deployment completed" -ForegroundColor Green
    }
    else {
        Write-Host "Deployment failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "Waiting for services..." -ForegroundColor Blue
    
    kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SQL Server: Ready" -ForegroundColor Green
    }
    else {
        Write-Host "SQL Server: Timeout" -ForegroundColor Yellow
    }
    
    kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Namespace
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Web App: Ready" -ForegroundColor Green
    }
    else {
        Write-Host "Web App: Timeout" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Deployment Status:" -ForegroundColor Blue
    kubectl get all -n $Namespace
    
    Write-Host ""
    Write-Host "Pod Status:" -ForegroundColor Blue
    kubectl get pods -n $Namespace -o wide
    
    Write-Host ""
    Write-Host "Access Information:" -ForegroundColor Green
    Write-Host "1. Run port-forward:" -ForegroundColor Cyan
    Write-Host "   kubectl port-forward svc/property-management-service 8080:80 -n $Namespace"
    Write-Host "2. Open browser: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "3. Login: Admin / 01Password2025#" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Operation completed!" -ForegroundColor Green