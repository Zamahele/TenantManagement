# Absolute Hosting Windows SSD Deployment Guide

## ?? **Hosting Provider Details**

**Provider**: Absolute Hosting  
**Package**: .NET Core Palladium (Windows SSD)  
**Server Type**: Windows Server with IIS

## ?? **Access Credentials**

### **Control Panel Access**
- **URL**: https://solid.zadns.co.za
- **Username**: `gcweprop`
- **Password**: `u2:fDX95r1NDw:`

### **FTP Details**
- **Server**: `ftp.gcweproperty.co.za` or `gcweproperty.co.za.host-za.com`
- **Username**: `gcweprop`
- **Password**: `u2:fDX95r1NDw:`
- **Upload Directory**: `/wwwroot/` ? **IMPORTANT**

### **Website URLs**
- **Production**: http://gcweproperty.co.za
- **Temporary/Testing**: http://gcweproperty.co.za.host-za.com
- **Email Access**: http://mail.gcweproperty.co.za

## ?? **Required GitHub Secrets**

Set these in your GitHub repository (Settings ? Secrets and variables ? Actions):

```
FTP_SERVER = ftp.gcweproperty.co.za
FTP_USERNAME = gcweprop
FTP_PASSWORD = u2:fDX95r1NDw:

DB_SERVER = mart.zadns.co.za
DB_USERNAME = [your database username]
DB_PASSWORD = [your database password]
```

## ?? **Automated Deployment Process**

Your GitHub Actions workflow now automatically:

1. ? **Builds and tests** your application
2. ? **Creates web.config** for Windows IIS hosting
3. ? **Configures production settings** with database credentials
4. ? **Uploads to FTP** directly to `/wwwroot/` directory
5. ? **Tests application startup** via multiple URLs
6. ? **Applies database migrations** if endpoints are available

## ?? **Server File Structure**

```
/wwwroot/                           # Application root (DO NOT DELETE)
??? PropertyManagement.Web.dll     # Main application
??? web.config                     # IIS configuration
??? appsettings.Production.json    # Production settings
??? logs/                          # Application logs
?   ??? propertymanagement.log    # Current log
?   ??? stdout/                    # IIS logs
??? wwwroot/                       # Static files (CSS, JS, images)
??? setup-logs.ps1                # Log setup script
```

## ?? **First-Time Setup Steps**

### **1. Verify Deployment**
- Visit: http://gcweproperty.co.za (production)
- Or: http://gcweproperty.co.za.host-za.com (temporary)

### **2. Database Setup**
If database isn't automatically configured:
1. **Access control panel**: https://solid.zadns.co.za
2. **Create database** in the hosting panel
3. **Run database script**: Use `database-scripts/create-production-database.sql`
4. **Test connection**: Check `/api/migration/status`

### **3. Application Login**
- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`

## ?? **Domain Configuration**

### **DNS Settings (if needed)**
If your domain is managed elsewhere, update name servers to:
```
ns1.mydnscloud.co.za
ns2.mydnscloud.co.za
ns3.mydnscloud.co.za
ns4.mydnscloud.co.za
```

### **SSL Certificate**
- Configure SSL in the hosting control panel
- Access via: https://solid.zadns.co.za

## ?? **Email Configuration**

### **Webmail Access**
- **URL**: http://mail.gcweproperty.co.za
- **Setup**: Configure email accounts in control panel

### **Email Server Settings**
- **Documentation**: Available in hosting knowledge base
- **Support Article**: Email setup instructions provided

## ?? **Monitoring & Troubleshooting**

### **Application Health**
- **Health Check**: `/health`
- **Migration Status**: `/api/migration/status`
- **Metrics**: `/metrics`

### **Log Files**
- **Application Logs**: `/wwwroot/logs/propertymanagement.log`
- **IIS Logs**: `/wwwroot/logs/stdout/`
- **Daily Rotation**: Automatic log file rotation

### **Common Issues**

**Application not loading?**
1. Check IIS is running in control panel
2. Verify .NET 8 runtime is enabled
3. Check file permissions in `/wwwroot/`
4. Review IIS logs in control panel

**Database connection issues?**
1. Verify database exists in hosting panel
2. Check connection string in `appsettings.Production.json`
3. Test database connectivity from control panel
4. Ensure database user has proper permissions

**FTP deployment fails?**
1. Verify FTP credentials are correct
2. Check `/wwwroot/` directory exists
3. Ensure FTP user has write permissions
4. Try alternative FTP server: `gcweproperty.co.za.host-za.com`

## ?? **Support Resources**

### **Absolute Hosting Support**
- **Status Page**: Check service status page
- **WhatsApp**: Available for outages and emergencies
- **Forum**: MYBB Forum (no login required)
- **Knowledge Base**: Available in client area

### **Email Security**
- **IP Blocking**: Automatic security may block IPs
- **Unblock Guide**: Knowledge base article for IP unblocking
- **Article**: https://client.absolutehosting.co.za/knowledgebase/403/

## ?? **Development Workflow**

1. **Make changes** in your local development environment
2. **Create migrations** if database changes are needed:
   ```bash
   dotnet ef migrations add YourMigrationName --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
3. **Commit and push** to GitHub
4. **GitHub Actions** automatically deploys to production
5. **Verify deployment** at http://gcweproperty.co.za
6. **Check logs** if any issues occur

## ? **Deployment Checklist**

- [ ] GitHub secrets configured with hosting credentials
- [ ] Database server accessible and configured
- [ ] Domain DNS configured (if applicable)
- [ ] SSL certificate configured (optional)
- [ ] Email accounts configured (if needed)
- [ ] Application tested and admin login working
- [ ] Monitoring endpoints responding
- [ ] Log files being created properly

Your Property Management System is now deployed on professional Windows SSD hosting with full CI/CD automation! ??