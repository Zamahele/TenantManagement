#!/usr/bin/env pwsh

# Property Management System - Final Working Kubernetes Deployment Script
# Tested and verified configuration - August 25, 2025

param(
    [switch]$CleanDeploy,
    [string]$GitHubToken = $env:GITHUB_TOKEN,
    [string]$GitHubEmail = $env:GITHUB_EMAIL
)

$ErrorActionPreference = "Stop"
$Namespace = "property-management"

Write-Host "🎯 Property Management System - Final Kubernetes Deployment" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Green

# Verify prerequisites
Write-Host "🔍 Verifying prerequisites..." -ForegroundColor Cyan

# Check kubectl
if (!(Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Error "❌ kubectl is not installed or not in PATH"
    exit 1
}

# Check SSL certificate
if (!(Test-Path "aspnetapp.pfx")) {
    Write-Error "❌ SSL certificate file 'aspnetapp.pfx' not found in current directory"
    exit 1
}

# Test cluster connectivity
try {
    kubectl cluster-info | Out-Null
    Write-Host "✅ Kubernetes cluster accessible" -ForegroundColor Green
} catch {
    Write-Error "❌ Cannot connect to Kubernetes cluster"
    exit 1
}

# Clean deployment if requested
if ($CleanDeploy) {
    Write-Host "🧹 Performing clean deployment..." -ForegroundColor Yellow
    kubectl delete namespace $Namespace --ignore-not-found=true
    Write-Host "⏳ Waiting for complete cleanup..." -ForegroundColor Yellow
    
    $timeout = 60
    $elapsed = 0
    do {
        Start-Sleep -Seconds 2
        $elapsed += 2
        $namespaceExists = kubectl get namespace $Namespace --ignore-not-found=true 2>$null
        if ($elapsed -gt $timeout) {
            Write-Warning "Cleanup timeout - proceeding anyway"
            break
        }
    } while ($namespaceExists)
    Write-Host "✅ Cleanup completed" -ForegroundColor Green
}

try {
    Write-Host "🚀 Starting deployment..." -ForegroundColor Cyan
    
    # Step 1: Create namespace and base resources
    Write-Host "📁 Creating namespace and base resources..." -ForegroundColor White
    kubectl apply -f k8s/01-namespace.yaml
    kubectl apply -f k8s/02-secrets.yaml
    kubectl apply -f k8s/03-configmap.yaml
    kubectl apply -f k8s/04-storage.yaml
    
    # Step 2: Create SSL certificate secret
    Write-Host "🔒 Creating SSL certificate secret..." -ForegroundColor White
    kubectl create secret generic https-cert `
        --from-file=aspnetapp.pfx=aspnetapp.pfx `
        -n $Namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Step 3: Create GHCR authentication secret
    Write-Host "🐙 Creating GitHub Container Registry secret..." -ForegroundColor White
    kubectl create secret docker-registry ghcr-secret `
        --docker-server=ghcr.io `
        --docker-username=$GitHubEmail `
        --docker-password=$GitHubToken `
        --docker-email=$GitHubEmail `
        -n $Namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Step 4: Deploy SQL Server
    Write-Host "🗄️  Deploying SQL Server..." -ForegroundColor White
    kubectl apply -f k8s/05-sql-server.yaml
    
    # Step 5: Wait for SQL Server to be ready
    Write-Host "⏳ Waiting for SQL Server to be ready (max 5 minutes)..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $Namespace
    Write-Host "✅ SQL Server is ready" -ForegroundColor Green
    
    # Step 6: Deploy web application and services
    Write-Host "🌐 Deploying web application..." -ForegroundColor White
    kubectl apply -f k8s/06-web-app.yaml
    kubectl apply -f k8s/07-services.yaml
    
    # Step 7: Wait for web application to be ready
    Write-Host "⏳ Waiting for web application to be ready (max 5 minutes)..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $Namespace
    Write-Host "✅ Web application is ready" -ForegroundColor Green
    
    # Step 8: Deploy optional ingress if exists
    if (Test-Path "k8s/08-ingress.yaml") {
        Write-Host "🌍 Creating ingress..." -ForegroundColor White
        kubectl apply -f k8s/08-ingress.yaml
    }
    
    Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Green
    
    # Show deployment status
    Write-Host "`n📊 Deployment Status:" -ForegroundColor Cyan
    Write-Host "======================" -ForegroundColor Cyan
    kubectl get all -n $Namespace
    
    Write-Host "`n🌐 Access Information:" -ForegroundColor Cyan
    Write-Host "======================" -ForegroundColor Cyan
    Write-Host "1. Start port forward:" -ForegroundColor White
    Write-Host "   kubectl port-forward svc/property-management-service 8443:443 -n $Namespace" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2. Access URL:" -ForegroundColor White
    Write-Host "   https://localhost:8443" -ForegroundColor Green
    Write-Host ""
    Write-Host "3. Login Credentials:" -ForegroundColor White
    Write-Host "   Username: Admin" -ForegroundColor Cyan
    Write-Host "   Password: 01Pa`$`$w0rd2025#" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "4. Browser Certificate Warning:" -ForegroundColor White
    Write-Host "   Click 'Advanced' → 'Proceed to localhost (unsafe)'" -ForegroundColor Yellow
    
    Write-Host "`n📋 Useful Commands:" -ForegroundColor Cyan
    Write-Host "===================" -ForegroundColor Cyan
    Write-Host "View logs:        kubectl logs -f deployment/property-management-web -n $Namespace" -ForegroundColor White
    Write-Host "Check pods:       kubectl get pods -n $Namespace" -ForegroundColor White
    Write-Host "Scale app:        kubectl scale deployment property-management-web --replicas=3 -n $Namespace" -ForegroundColor White
    Write-Host "Clean delete:     kubectl delete namespace $Namespace" -ForegroundColor White
    
    Write-Host "`n🎯 Property Management System is ready for use!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Deployment failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    Write-Host "`n🔍 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "Check pods:   kubectl get pods -n $Namespace" -ForegroundColor White
    Write-Host "Check events: kubectl get events -n $Namespace --sort-by='.lastTimestamp'" -ForegroundColor White
    Write-Host "Check logs:   kubectl logs -f deployment/property-management-web -n $Namespace" -ForegroundColor White
    
    exit 1
}