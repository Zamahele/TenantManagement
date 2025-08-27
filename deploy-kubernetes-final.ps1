#!/usr/bin/env pwsh

# Property Management System - Kubernetes Deployment Script

param(
    [switch]$CleanDeploy
)

$ErrorActionPreference = "Stop"
$Namespace = "property-management"

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
    
    # Deploy SQL Server
    Write-Host "Deploying SQL Server..."
    kubectl apply -f k8s/05-sql-server.yaml
    
    # Wait for SQL Server
    Write-Host "Waiting for SQL Server to be ready..."
    kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace
    
    # Deploy web application
    Write-Host "Deploying web application..."
    kubectl apply -f k8s/06-web-app.yaml
    kubectl apply -f k8s/07-services.yaml
    
    # Wait for web application
    Write-Host "Waiting for web application to be ready..."
    kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Namespace
    
    # Deploy ingress if available
    if (Test-Path "k8s/08-ingress.yaml") {
        Write-Host "Creating ingress..."
        kubectl apply -f k8s/08-ingress.yaml
    }
    
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    
    # Show status
    Write-Host "`nDeployment Status:" -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    
    Write-Host "`nAccess Information:" -ForegroundColor Cyan
    Write-Host "Port forward: kubectl port-forward svc/property-management-service 8080:80 -n $Namespace"
    Write-Host "Then access: http://localhost:8080"
    Write-Host "Login - Username: Admin, Password: 01Pa`$`$w0rd2025#"
    
} catch {
    Write-Host "Deployment failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host "`nTroubleshooting commands:" -ForegroundColor Yellow
    Write-Host "kubectl get pods -n $Namespace"
    Write-Host "kubectl logs -f deployment/property-management-web -n $Namespace"
    Write-Host "kubectl get events -n $Namespace --sort-by=.lastTimestamp"
    
    exit 1
}