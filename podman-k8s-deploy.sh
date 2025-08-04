#!/bin/bash

set -e

echo "🚀 Property Management System - Podman + Kubernetes Deployment"
echo "============================================================="

# Configuration
IMAGE_NAME="property-management"
IMAGE_TAG="latest"
NAMESPACE="property-management"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_step() {
    echo -e "${BLUE}🔵 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check prerequisites
print_step "Checking prerequisites..."

if ! command -v podman &> /dev/null; then
    print_error "Podman is not installed. Please install Podman first."
    exit 1
fi

if ! command -v kubectl &> /dev/null; then
    print_error "kubectl is not installed. Please install kubectl first."
    exit 1
fi

print_success "Prerequisites check passed"

# Step 1: Build Docker image with Podman
print_step "Building Docker image with Podman..."

podman build -t $IMAGE_NAME:$IMAGE_TAG -f PropertyManagement.Web/Dockerfile .

if [ $? -eq 0 ]; then
    print_success "Image built successfully: $IMAGE_NAME:$IMAGE_TAG"
else
    print_error "Failed to build image"
    exit 1
fi

# Step 2: Verify image
print_step "Verifying built image..."
podman images | grep $IMAGE_NAME
print_success "Image verification complete"

# Step 3: Check if Kubernetes cluster is accessible
print_step "Checking Kubernetes cluster connectivity..."

if kubectl cluster-info &> /dev/null; then
    print_success "Kubernetes cluster is accessible"
else
    print_error "Cannot connect to Kubernetes cluster. Please check your kubectl configuration."
    exit 1
fi

# Step 4: Create namespace if it doesn't exist
print_step "Setting up Kubernetes namespace..."

if kubectl get namespace $NAMESPACE &> /dev/null; then
    print_warning "Namespace $NAMESPACE already exists"
else
    kubectl create namespace $NAMESPACE
    print_success "Namespace $NAMESPACE created"
fi

# Step 5: Deploy to Kubernetes
print_step "Deploying to Kubernetes..."

kubectl apply -f kubernetes/

if [ $? -eq 0 ]; then
    print_success "Kubernetes manifests applied successfully"
else
    print_error "Failed to apply Kubernetes manifests"
    exit 1
fi

# Step 6: Wait for SQL Server to be ready
print_step "Waiting for SQL Server to be ready..."

kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n $NAMESPACE

if [ $? -eq 0 ]; then
    print_success "SQL Server is ready"
else
    print_warning "SQL Server readiness check timed out, but continuing..."
fi

# Step 7: Wait for web application to be ready
print_step "Waiting for web application to be ready..."

kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n $NAMESPACE

if [ $? -eq 0 ]; then
    print_success "Web application is ready"
else
    print_warning "Web application readiness check timed out"
fi

# Step 8: Display deployment status
print_step "Checking deployment status..."

echo ""
echo "📊 Deployment Status:"
echo "===================="
kubectl get all -n $NAMESPACE

echo ""
echo "📋 Pods Status:"
echo "==============="
kubectl get pods -n $NAMESPACE -o wide

# Step 9: Get service information
print_step "Getting service information..."

echo ""
echo "🌐 Service Information:"
echo "======================"
kubectl get svc -n $NAMESPACE

# Try to get external IP for LoadBalancer
EXTERNAL_IP=$(kubectl get svc property-management-service -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null)

if [ -n "$EXTERNAL_IP" ]; then
    print_success "Application is accessible at: http://$EXTERNAL_IP"
else
    print_warning "LoadBalancer external IP not assigned yet. You can use port-forward:"
    echo "kubectl port-forward svc/property-management-service 8080:80 -n $NAMESPACE"
fi

# Step 10: Show logs
print_step "Recent application logs:"
echo "========================"
kubectl logs -l app=property-management-web -n $NAMESPACE --tail=10 || print_warning "Could not retrieve logs"

echo ""
print_success "🎉 Deployment completed successfully!"
echo ""
echo "📝 Next Steps:"
echo "=============="
echo "1. Access the application:"
echo "   - If LoadBalancer IP is available: http://\$EXTERNAL_IP"
echo "   - Otherwise use port-forward: kubectl port-forward svc/property-management-service 8080:80 -n $NAMESPACE"
echo ""
echo "2. Default login credentials:"
echo "   - Username: Admin"
echo "   - Password: 01Pa\$\$w0rd2025#"
echo ""
echo "3. Monitor deployment:"
echo "   - kubectl get pods -n $NAMESPACE -w"
echo "   - kubectl logs -f deployment/property-management-web -n $NAMESPACE"
echo ""
echo "4. Cleanup (if needed):"
echo "   - kubectl delete namespace $NAMESPACE"
echo "   - podman rmi $IMAGE_NAME:$IMAGE_TAG"
echo ""

# Optional: Show ingress information if available
if kubectl get ingress -n $NAMESPACE &> /dev/null; then
    echo "🔗 Ingress Information:"
    echo "======================"
    kubectl get ingress -n $NAMESPACE
fi