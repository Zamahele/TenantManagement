# Kubernetes Deployment for Property Management System

## Overview
This guide provides complete Kubernetes manifests to deploy the Property Management System (.NET 8 Razor Pages application) to a Kubernetes cluster.

## Prerequisites
- Kubernetes cluster (v1.20+)
- kubectl configured
- Docker image built and pushed to registry  
- Ingress controller (optional, for external access)

## Quick Start

1. **Build and push your Docker image**:
```bash
docker build -t your-registry/property-management:latest -f PropertyManagement.Web/Dockerfile .
docker push your-registry/property-management:latest
```

2. **Update image reference** in the deployment YAML below
3. **Create manifest files** from the YAML sections below
4. **Deploy**:
```bash
kubectl apply -f .
```

## Kubernetes Manifests

### 1. Namespace (namespace.yaml)
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: property-management
  labels:
    app: property-management
    environment: production
```

### 2. Database Secrets (secrets.yaml)
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sql-server-secret
  namespace: property-management
type: Opaque
data:
  # Base64 encoded: Your_password123
  SA_PASSWORD: WW91cl9wYXNzd29yZDEyMw==
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
  namespace: property-management
type: Opaque
data:
  # Base64 encoded: Production
  ASPNETCORE_ENVIRONMENT: UHJvZHVjdGlvbg==
```

### 3. Application Configuration (configmap.yaml)
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
  namespace: property-management
data:
  appsettings.Production.json: |
    {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "EnableDatabaseSeeding": true,
        "ConnectionStrings": {
            "DefaultConnection": "Server=sql-server-service,1433;Database=PropertyManagementDb;User=sa;Password=Your_password123;MultipleActiveResultSets=true;TrustServerCertificate=True"
        },
        "AllowedHosts": "*",
        "UtilityRates": {
            "WaterPerLiter": 0.02,
            "ElectricityPerKwh": 1.50
        },
        "Serilog": {
            "MinimumLevel": "Information",
            "WriteTo": [
                {
                    "Name": "File",
                    "Args": {
                        "path": "/app/logs/propertymanagement.log",
                        "rollingInterval": "Day"
                    }
                },
                {
                    "Name": "Console"
                }
            ]
        }
    }
```

### 4. Persistent Storage (storage.yaml)
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sql-server-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
  storageClassName: standard  # Adjust for your cluster
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: app-logs-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi
  storageClassName: standard  # Adjust for your cluster
```

### 5. SQL Server Deployment (sql-server.yaml)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sql-server
  namespace: property-management
  labels:
    app: sql-server
spec:
  replicas: 1
  selector:
    matchLabels:
      app: sql-server
  template:
    metadata:
      labels:
        app: sql-server
    spec:
      containers:
      - name: sql-server
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
        - containerPort: 1433
          name: sql-port
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sql-server-secret
              key: SA_PASSWORD
        - name: MSSQL_PID
          value: "Express"
        volumeMounts:
        - name: sql-storage
          mountPath: /var/opt/mssql
        resources:
          requests:
            memory: "2Gi"
            cpu: "500m"
          limits:
            memory: "4Gi"
            cpu: "1"
        livenessProbe:
          exec:
            command:
            - /opt/mssql-tools/bin/sqlcmd
            - -S
            - localhost
            - -U
            - sa
            - -P
            - Your_password123
            - -Q
            - SELECT 1
          initialDelaySeconds: 60
          periodSeconds: 30
          timeoutSeconds: 10
        readinessProbe:
          exec:
            command:
            - /opt/mssql-tools/bin/sqlcmd
            - -S
            - localhost
            - -U
            - sa
            - -P
            - Your_password123
            - -Q
            - SELECT 1
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
      volumes:
      - name: sql-storage
        persistentVolumeClaim:
          claimName: sql-server-pvc
```

### 6. Property Management Web App (web-app.yaml)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: property-management-web
  namespace: property-management
  labels:
    app: property-management-web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: property-management-web
  template:
    metadata:
      labels:
        app: property-management-web
    spec:
      initContainers:
      - name: wait-for-db
        image: busybox:1.35
        command: 
        - sh
        - -c
        - |
          echo "Waiting for SQL Server to be ready..."
          until nc -z sql-server-service 1433; do
            echo "SQL Server not ready, waiting..."
            sleep 5
          done
          echo "SQL Server is ready!"
      containers:
      - name: web-app
        image: your-registry/property-management:latest  # UPDATE THIS IMAGE
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: ASPNETCORE_ENVIRONMENT
        - name: ASPNETCORE_URLS
          value: "http://+:80"
        - name: ConnectionStrings__DefaultConnection
          value: "Server=sql-server-service,1433;Database=PropertyManagementDb;User=sa;Password=Your_password123;MultipleActiveResultSets=true;TrustServerCertificate=True"
        volumeMounts:
        - name: app-config
          mountPath: /app/appsettings.Production.json
          subPath: appsettings.Production.json
          readOnly: true
        - name: app-logs
          mountPath: /app/logs
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /
            port: 80
            scheme: HTTP
          initialDelaySeconds: 60
          periodSeconds: 30
          timeoutSeconds: 10
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /
            port: 80
            scheme: HTTP
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /
            port: 80
            scheme: HTTP
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 30
      volumes:
      - name: app-config
        configMap:
          name: app-config
      - name: app-logs
        persistentVolumeClaim:
          claimName: app-logs-pvc
```

### 7. Kubernetes Services (services.yaml)
```yaml
apiVersion: v1
kind: Service
metadata:
  name: sql-server-service
  namespace: property-management
  labels:
    app: sql-server
spec:
  selector:
    app: sql-server
  ports:
  - port: 1433
    targetPort: 1433
    protocol: TCP
    name: sql-port
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: property-management-service
  namespace: property-management
  labels:
    app: property-management-web
spec:
  selector:
    app: property-management-web
  ports:
  - port: 80
    targetPort: 80
    protocol: TCP
    name: http
  - port: 443
    targetPort: 443
    protocol: TCP
    name: https
  type: LoadBalancer
```

### 8. Ingress Controller (ingress.yaml) - Optional
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: property-management-ingress
  namespace: property-management
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/ssl-redirect: "false"
    # nginx.ingress.kubernetes.io/ssl-redirect: "true"  # Enable for HTTPS
    # cert-manager.io/cluster-issuer: "letsencrypt-prod"  # For automatic SSL
spec:
  # tls:  # Uncomment for HTTPS
  # - hosts:
  #   - property-management.your-domain.com
  #   secretName: property-management-tls
  rules:
  - host: property-management.your-domain.com  # UPDATE THIS DOMAIN
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: property-management-service
            port:
              number: 80
```

## Deployment Instructions

### Step 1: Prepare Docker Image
```bash
# Build the image
docker build -t your-registry/property-management:latest -f PropertyManagement.Web/Dockerfile .

# Push to registry
docker push your-registry/property-management:latest
```

### Step 2: Update Configuration
1. **Image Reference**: Update the image in `web-app.yaml`
2. **Domain**: Update domain in `ingress.yaml` if using ingress
3. **Storage Class**: Update `storageClassName` in `storage.yaml` if needed
4. **Passwords**: Update database password in `secrets.yaml` for production

### Step 3: Create Manifest Files
Create individual YAML files from each section above:
- `01-namespace.yaml`
- `02-secrets.yaml`
- `03-configmap.yaml`
- `04-storage.yaml`
- `05-sql-server.yaml`
- `06-web-app.yaml`
- `07-services.yaml`
- `08-ingress.yaml` (optional)

### Step 4: Deploy to Kubernetes
```bash
# Deploy infrastructure first
kubectl apply -f 01-namespace.yaml
kubectl apply -f 02-secrets.yaml
kubectl apply -f 03-configmap.yaml
kubectl apply -f 04-storage.yaml

# Deploy SQL Server
kubectl apply -f 05-sql-server.yaml

# Wait for SQL Server to be ready
kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n property-management

# Deploy the web application
kubectl apply -f 06-web-app.yaml
kubectl apply -f 07-services.yaml

# Optional: Deploy ingress
kubectl apply -f 08-ingress.yaml
```

### Step 5: Verify Deployment
```bash
# Check all resources
kubectl get all -n property-management

# Check pod status
kubectl get pods -n property-management

# Check logs
kubectl logs -f deployment/property-management-web -n property-management
kubectl logs -f deployment/sql-server -n property-management

# Get external IP (if using LoadBalancer)
kubectl get svc property-management-service -n property-management
```

## Accessing the Application

### Via LoadBalancer Service
```bash
# Get external IP
kubectl get svc property-management-service -n property-management

# Access via: http://<EXTERNAL-IP>
```

### Via Ingress (if configured)
- Access via: `http://property-management.your-domain.com`
- Ensure DNS points to your ingress controller's IP

### Default Login Credentials
- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`

## Monitoring and Maintenance

### View Logs
```bash
# Application logs
kubectl logs -f deployment/property-management-web -n property-management

# Database logs  
kubectl logs -f deployment/sql-server -n property-management

# View persistent logs
kubectl exec -it deployment/property-management-web -n property-management -- cat /app/logs/propertymanagement.log
```

### Scaling
```bash
# Scale web application
kubectl scale deployment property-management-web --replicas=5 -n property-management

# Monitor scaling
kubectl get pods -n property-management -w
```

### Health Checks
```bash
# Check deployment status
kubectl get deployments -n property-management

# Check pod health
kubectl describe pods -n property-management

# Test application endpoint
kubectl port-forward svc/property-management-service 8080:80 -n property-management
# Then access http://localhost:8080
```

## Troubleshooting

### Common Issues

1. **Pods not starting**:
   ```bash
   kubectl describe pod <pod-name> -n property-management
   kubectl logs <pod-name> -n property-management
   ```

2. **Database connection failed**:
   - Check SQL Server pod status
   - Verify connection string in configmap
   - Test connectivity: `kubectl exec -it <web-pod> -n property-management -- nc -z sql-server-service 1433`

3. **Storage issues**:
   ```bash
   kubectl get pv,pvc -n property-management
   kubectl describe pvc sql-server-pvc -n property-management
   ```

4. **Service not accessible**:
   ```bash
   kubectl get svc -n property-management
   kubectl describe svc property-management-service -n property-management
   ```

### Reset Deployment
```bash
# Delete everything
kubectl delete namespace property-management

# Redeploy
kubectl apply -f .
```

## Production Considerations

1. **Security**:
   - Use proper secrets management (Azure Key Vault, AWS Secrets Manager, etc.)
   - Enable network policies
   - Use service mesh for secure communication

2. **High Availability**:
   - Use multiple replicas for web app
   - Consider SQL Server Always On or managed database service
   - Deploy across multiple availability zones

3. **Backup & Recovery**:
   - Implement regular database backups
   - Store persistent volumes snapshots
   - Test disaster recovery procedures

4. **SSL/TLS**:
   - Configure proper SSL certificates
   - Use cert-manager for automatic certificate management
   - Enable HTTPS redirect in ingress

5. **Resource Management**:
   - Set appropriate resource requests and limits
   - Use horizontal pod autoscaler (HPA)
   - Monitor resource usage with Prometheus/Grafana

6. **Logging & Monitoring**:
   - Configure centralized logging (ELK stack)
   - Set up application performance monitoring
   - Configure alerts for critical metrics

This deployment provides a solid foundation for running the Property Management System in Kubernetes with proper separation of concerns, health checks, and scalability options.