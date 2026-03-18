# Quick Setup Guide

This guide will help you get the Chat Support System running quickly.

## 🚀 Quick Start (Recommended)

### Using Docker for Dependencies Only

The easiest way to run the required services (SQL Server and Redis):

1. **Install Docker Desktop**
   - Download from [docker.com](https://www.docker.com/products/docker-desktop)

2. **Start Services**
   ```bash
   docker-compose up -d sqlserver redis
   ```

3. **Verify Services**
   ```bash
   # Check if running
   docker-compose ps
   
   # Test Redis
   docker exec -it chat-redis redis-cli ping
   # Should return: PONG
   ```

4. **Update Connection Strings**
   
   Edit `Backend/ChatSupportApi/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=ChatSupportDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;",
       "Redis": "localhost:6379"
     }
   }
   ```

5. **Run Backend**
   ```bash
   cd Backend/ChatSupportApi
   dotnet run
   ```

6. **Run Frontend** (in new terminal)
   ```bash
   cd Frontend
   npm install
   npm install @microsoft/signalr
   ng serve
   ```

7. **Access Application**
   - Frontend: http://localhost:4200
   - Backend API: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger

## 🪟 Windows Local Setup (Without Docker)

### Prerequisites

1. **SQL Server LocalDB** (included with Visual Studio)
   - Or install SQL Server Express from [microsoft.com](https://www.microsoft.com/sql-server/sql-server-downloads)

2. **Redis for Windows**
   - Option 1: Install [Memurai](https://www.memurai.com/get-memurai) (Free Developer edition)
   - Option 2: Use WSL2 with Ubuntu and install Redis

3. **.NET 7.0 SDK**
   ```bash
   winget install Microsoft.DotNet.SDK.7
   ```

4. **Node.js 16+**
   ```bash
   winget install OpenJS.NodeJS
   ```

### Setup Steps

1. **Start Redis** (Memurai)
   - Run Memurai from Start Menu, or
   - `memurai` command in terminal

2. **Verify SQL Server**
   ```bash
   # Check LocalDB instances
   sqllocaldb info
   
   # Start LocalDB if needed
   sqllocaldb start mssqllocaldb
   ```

3. **Run Backend**
   ```bash
   cd Backend/ChatSupportApi
   dotnet restore --configfile NuGet.config
   dotnet run
   ```
   
   The database will be created automatically on first run.

4. **Run Frontend**
   ```bash
   cd Frontend
   npm install
   npm install @microsoft/signalr
   ng serve --open
   ```

## 🐧 Linux Setup

### Install Dependencies

```bash
# Update packages
sudo apt update

# Install .NET 7.0
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 7.0

# Install Node.js
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install Redis
sudo apt-get install redis-server

# Start Redis
sudo systemctl start redis-server
sudo systemctl enable redis-server

# Install SQL Server (Optional - or use Docker)
# Follow: https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-ubuntu
```

### Run Application

Follow the same steps as Docker setup above, starting from step 4.

## 🍎 macOS Setup

### Install Dependencies

```bash
# Install Homebrew (if not installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install .NET 7.0
brew install --cask dotnet-sdk

# Install Node.js
brew install node

# Install Redis
brew install redis

# Start Redis
brew services start redis
```

### SQL Server Options

- **Option 1:** Use Docker (recommended)
  ```bash
  docker-compose up -d sqlserver
  ```

- **Option 2:** Use Azure SQL Database or remote SQL Server

### Run Application

Follow the same steps as Docker setup above, starting from step 4.

## 🔧 Configuration

### Environment Variables

You can use environment variables instead of modifying appsettings.json:

```bash
# Windows CMD
set ConnectionStrings__DefaultConnection=Server=...
set ConnectionStrings__Redis=localhost:6379

# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Server=..."
$env:ConnectionStrings__Redis="localhost:6379"

# Linux/macOS
export ConnectionStrings__DefaultConnection="Server=..."
export ConnectionStrings__Redis="localhost:6379"
```

### Angular Environment

Edit `Frontend/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  hubUrl: 'https://localhost:5001/chathub'
};
```

For production, edit `environment.prod.ts` accordingly.

## 🧪 Verify Setup

### 1. Check Redis

```bash
redis-cli ping
# Should return: PONG
```

### 2. Check SQL Server

```bash
# For LocalDB
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"

# For Docker
docker exec -it chat-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT @@VERSION"
```

### 3. Test API

```bash
curl https://localhost:5001/api/team
# Should return JSON array of teams
```

### 4. Test Frontend

Navigate to http://localhost:4200 - you should see the chat interface.

## 🐛 Common Issues

### Issue: "Connection reset by peer" (Redis)

**Solution:** Ensure Redis is running
```bash
# Check status
redis-cli ping

# Start Redis
# Windows (Memurai): memurai
# Linux: sudo systemctl start redis-server
# macOS: brew services start redis
# Docker: docker-compose up -d redis
```

### Issue: "Database connection failed"

**Solution:** 
1. Check SQL Server is running
2. Verify connection string
3. Check firewall settings
4. For Docker, ensure container is healthy: `docker-compose ps`

### Issue: "CORS policy blocked"

**Solution:** Ensure the Angular dev server URL (http://localhost:4200) is in the CORS policy in `Program.cs`

### Issue: "Port already in use"

**Solution:**
```bash
# Find process using port (Windows)
netstat -ano | findstr :5001

# Kill process
taskkill /PID <pid> /F

# Or use different ports
dotnet run --urls "https://localhost:5002;http://localhost:5003"
ng serve --port 4201
```

## 📦 Production Deployment

### Backend Deployment

1. **Publish the application**
   ```bash
   cd Backend/ChatSupportApi
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to:**
   - Azure App Service
   - AWS Elastic Beanstalk
   - IIS (Windows Server)
   - Linux with systemd service
   - Docker container

### Frontend Deployment

1. **Build for production**
   ```bash
   cd Frontend
   ng build --configuration production
   ```

2. **Deploy `dist/` folder to:**
   - Azure Static Web Apps
   - AWS S3 + CloudFront
   - Netlify
   - Vercel
   - Any static hosting

### Database Migration

For production, use proper migrations:

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script -o migration.sql
```

### Environment Configuration

Set production connection strings via:
- Azure App Settings
- AWS Systems Manager Parameter Store
- Environment variables
- Azure Key Vault / AWS Secrets Manager

## 📊 Monitoring

### Health Checks

Add health check endpoints:

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChatSupportDbContext>()
    .AddRedis(redisConnectionString);

app.MapHealthChecks("/health");
```

### Logging

Configure logging providers:
- Application Insights (Azure)
- CloudWatch (AWS)
- Elasticsearch + Kibana
- Seq

### Metrics

Monitor:
- Queue length
- Agent utilization
- Session completion rate
- Average wait time
- System performance

## 🔒 Security Checklist

Before production deployment:

- [ ] Enable HTTPS only
- [ ] Add authentication (JWT, OAuth2)
- [ ] Implement rate limiting
- [ ] Secure Redis with password
- [ ] Use strong SQL Server credentials
- [ ] Enable audit logging
- [ ] Implement input validation
- [ ] Add CSRF protection
- [ ] Configure security headers
- [ ] Regular security updates

## 💡 Tips

1. **Development:** Use Docker for dependencies only, run code locally for easier debugging
2. **Testing:** Use separate databases for dev/test/prod
3. **Performance:** Enable Redis persistence in production
4. **Scalability:** Use Redis Cluster for high availability
5. **Monitoring:** Set up alerts for queue buildup and agent unavailability

## 📞 Need Help?

- Check application logs in console output
- Review Swagger documentation at `/swagger`
- Enable detailed logging in appsettings.json
- Check Docker container logs: `docker-compose logs -f`
