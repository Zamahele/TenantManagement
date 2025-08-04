# Property Management System - Podman + Kubernetes Deployment Guide

## 🚀 Quick Start

Deploy the Property Management System to Kubernetes using Podman with a single command:

```bash
./podman-k8s-deploy.sh
```

## 📋 Prerequisites

### Required Tools
- **Podman** (latest version recommended)
- **kubectl** configured with access to your Kubernetes cluster
- **Kubernetes cluster** (local: kind, k3s, minikube, or remote: AKS, EKS, GKE)

### Installation Commands

#### Podman Installation
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install podman

# RHEL/CentOS/Fedora
sudo dnf install podman

# macOS
brew install podman

# Windows
winget install RedHat.Podman
```

#### kubectl Installation
```bash
# Linux
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# macOS
brew install kubectl

# Windows
winget install Kubernetes.kubectl
```

## 🏗️ Architecture Overview

The deployment creates the following components in Kubernetes:

- **Namespace**: `property-management` (isolated environment)
- **SQL Server**: Persistent database with 20GB storage
- **Web Application**: ASP.NET Core app with 2 replicas
- **Services**: Internal (ClusterIP) and external (LoadBalancer) access
- **ConfigMaps**: Application configuration
- **Secrets**: Database passwords and sensitive data
- **Persistent Volumes**: Data persistence for database and logs

## 🚀 Deployment Methods

### Method 1: Automated Deployment (Recommended)

```bash
# Clone repository
git clone <repository-url>
cd TenantManagement

# Run automated deployment
./podman-k8s-deploy.sh
```

### Method 2: Manual Step-by-Step

#### Step 1: Build Container Image
```bash
# Build with Podman
podman build -t property-management:latest -f PropertyManagement.Web/Dockerfile .

# Verify image
podman images | grep property-management
```

#### Step 2: Deploy to Kubernetes
```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/podman-deployment.yaml

# Verify deployment
kubectl get all -n property-management
```

#### Step 3: Wait for Services
```bash
# Wait for SQL Server
kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n property-management

# Wait for Web App
kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n property-management
```

#### Step 4: Access Application
```bash
# Check service status
kubectl get svc -n property-management

# Port forward for local access
kubectl port-forward svc/property-management-service 8080:80 -n property-management

# Access at: http://localhost:8080
```

## 🔧 Configuration Options

### Environment Variables
The deployment can be customized by modifying the ConfigMap in `k8s/podman-deployment.yaml`:

- **Database Connection**: Modify `ConnectionStrings__DefaultConnection`
- **Logging Level**: Adjust `Serilog.MinimumLevel`
- **Application Settings**: Update `appsettings.Production.json`

### Resource Limits
Adjust resource requests and limits in the deployment:

```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "250m"
  limits:
    memory: "1Gi"
    cpu: "500m"
```

### Scaling
```bash
# Scale web application
kubectl scale deployment property-management-web --replicas=5 -n property-management

# Enable auto-scaling
kubectl autoscale deployment property-management-web --cpu-percent=70 --min=2 --max=10 -n property-management
```

## 🔍 Monitoring and Troubleshooting

### Check Deployment Status
```bash
# Overview of all resources
kubectl get all -n property-management

# Detailed pod information
kubectl describe pods -n property-management

# Check events
kubectl get events -n property-management --sort-by='.lastTimestamp'
```

### View Logs
```bash
# Web application logs
kubectl logs -f deployment/property-management-web -n property-management

# SQL Server logs
kubectl logs -f deployment/sql-server -n property-management

# All pods logs
kubectl logs -f -l app=property-management-web -n property-management
```

### Common Issues and Solutions

#### Issue: Image Pull Errors
```bash
# Check if image exists locally
podman images | grep property-management

# Rebuild if necessary
podman build -t property-management:latest -f PropertyManagement.Web/Dockerfile .
```

#### Issue: Database Connection Failures
```bash
# Check SQL Server status
kubectl get pods -l app=sql-server -n property-management

# Check SQL Server logs
kubectl logs -f deployment/sql-server -n property-management

# Test database connectivity
kubectl exec -it deployment/sql-server -n property-management -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Your_password123 -Q "SELECT 1"
```

#### Issue: Service Not Accessible
```bash
# Check service configuration
kubectl get svc property-management-service -n property-management -o yaml

# Use port-forward as alternative
kubectl port-forward svc/property-management-service 8080:80 -n property-management
```

## 🌐 Access Methods

### Local Access (Development)
```bash
# Port forward
kubectl port-forward svc/property-management-service 8080:80 -n property-management

# Access: http://localhost:8080
```

### LoadBalancer Access (Cloud)
```bash
# Get external IP
kubectl get svc property-management-service -n property-management

# Access: http://<EXTERNAL-IP>
```

### Ingress Access (Production)
The deployment includes an Ingress resource. Configure your DNS to point `property-management.local` to your ingress controller.

## 🔐 Default Credentials

- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`

## 🧹 Cleanup

### Remove Deployment
```bash
# Delete namespace (removes all resources)
kubectl delete namespace property-management

# Remove local image
podman rmi property-management:latest
```

### Partial Cleanup
```bash
# Remove only application (keep namespace)
kubectl delete deployment property-management-web -n property-management
kubectl delete svc property-management-service -n property-management
```

## 🚀 Advanced Features

### Persistent Storage
- **Database**: 20GB persistent volume for SQL Server data
- **Logs**: 5GB persistent volume for application logs
- **File Uploads**: Application uploads stored in persistent volume

### Health Checks
- **Liveness Probe**: HTTP GET on `/` endpoint
- **Readiness Probe**: SQL Server connectivity check
- **Startup Probe**: Initial container health verification

### Security Features
- **Secrets Management**: Database passwords stored in Kubernetes secrets
- **Network Policies**: Namespace isolation (can be added)
- **RBAC**: Role-based access control (can be configured)

## 📊 Monitoring Integration

### Prometheus Metrics
The application exposes metrics at `/metrics` endpoint:

```bash
# Access metrics
kubectl port-forward svc/property-management-service 8080:80 -n property-management
curl http://localhost:8080/metrics
```

### Log Aggregation
Logs are structured and can be integrated with:
- **ELK Stack** (Elasticsearch, Logstash, Kibana)
- **Grafana Loki**
- **Fluentd/Fluent Bit**

## 🔄 CI/CD Integration

### GitLab CI Example
```yaml
build-and-deploy:
  script:
    - podman build -t property-management:$CI_COMMIT_SHA .
    - podman tag property-management:$CI_COMMIT_SHA property-management:latest
    - kubectl set image deployment/property-management-web web-app=property-management:$CI_COMMIT_SHA -n property-management
```

### GitHub Actions Example
```yaml
- name: Build with Podman
  run: podman build -t property-management:latest .
  
- name: Deploy to Kubernetes
  run: kubectl apply -f k8s/podman-deployment.yaml
```

## 📝 Best Practices

### Security
1. **Change default passwords** in production
2. **Use private container registry** for images
3. **Enable RBAC** for cluster access
4. **Configure network policies** for traffic isolation
5. **Regular security updates** for base images

### Performance
1. **Set appropriate resource limits** based on load testing
2. **Configure horizontal pod autoscaling** for high availability
3. **Use persistent volumes** for data that needs to survive pod restarts
4. **Monitor application metrics** and set up alerting

### Maintenance
1. **Regular backups** of persistent volumes
2. **Rolling updates** for zero-downtime deployments
3. **Health checks** configuration for automatic recovery
4. **Log rotation** and retention policies

---

For additional support and advanced configurations, refer to the main [CLAUDE.md](./CLAUDE.md) file and the comprehensive Kubernetes documentation in [PropertyManagement.Web/CLAUDE.md](./PropertyManagement.Web/CLAUDE.md).