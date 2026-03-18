# Chat Support System

A comprehensive enterprise-grade chat support system built with .NET Core, Angular, Redis, Entity Framework, and SQL Server. The system implements advanced queue management, intelligent agent assignment algorithms, and real-time communication capabilities.

## 🏗️ Architecture Overview

### Technology Stack

**Backend:**
- .NET Core 7.0 Web API
- Entity Framework Core 7.0 with SQL Server
- Redis for FIFO queue management
- SignalR for real-time communication
- Background services for monitoring and processing

**Frontend:**
- Angular (latest version)
- TypeScript
- RxJS for reactive programming
- SignalR client for WebSocket communication

### Key Features

✅ **FIFO Queue Management** - Priority-based queue using Redis  
✅ **Intelligent Agent Assignment** - Round-robin with seniority preference  
✅ **Capacity Management** - Dynamic calculation based on team composition  
✅ **Shift Management** - 3-shift system (8 hours each)  
✅ **Overflow Team Support** - Automatic activation during office hours  
✅ **Session Monitoring** - Automatic detection of inactive sessions  
✅ **Real-time Chat** - WebSocket-based communication via SignalR  
✅ **Polling System** - Client health monitoring every 1 second  

## 📋 Business Rules

### Team Configuration

| Team | Composition | Shift | Capacity |
|------|------------|-------|----------|
| Team A | 1 Team Lead, 2 Mid-Level, 1 Junior | Day (08:00-16:00) | 15 |
| Team B | 1 Senior, 1 Mid-Level, 2 Junior | Day (08:00-16:00) | 16 |
| Team C | 2 Mid-Level | Night (00:00-08:00) | 12 |
| Overflow | 6 Junior | Day (08:00-16:00) | 24 |

### Capacity Calculation

**Formula:** `Capacity = Σ(10 × Seniority Multiplier)` (rounded down)

**Seniority Multipliers:**
- Junior: 0.4 (max 4 concurrent chats)
- Mid-Level: 0.6 (max 6 concurrent chats)
- Senior: 0.8 (max 8 concurrent chats)
- Team Lead: 0.5 (max 5 concurrent chats)

**Example:**  
Team A = (1 × 10 × 0.5) + (2 × 10 × 0.6) + (1 × 10 × 0.4) = 5 + 12 + 4 = 21

### Queue Management

- **Max Queue Size:** `Capacity × 1.5` (rounded down)
- **Overflow Activation:** When queue is full during office hours (08:00-24:00)
- **Refused Policy:** Chat refused when queue full and no overflow available

### Agent Assignment Rules

1. **Round-robin within seniority levels**
2. **Priority order:** Junior → Mid-Level → Senior → Team Lead
3. **Rationale:** Keeps senior agents available to assist juniors
4. **Shift ending:** Agents complete current chats but receive no new assignments

### Session Monitoring

- **Polling Interval:** 1 second
- **Inactive Threshold:** 3 missed polls (3 seconds)
- **Automatic Actions:**
  - Mark session as inactive
  - Remove from queue
  - Release assigned agent

## 🚀 Getting Started

### Prerequisites

1. **.NET 7.0 SDK**
   ```bash
   dotnet --version
   ```

2. **Node.js 16+** and **npm**
   ```bash
   node --version
   npm --version
   ```

3. **SQL Server** (LocalDB or full instance)
   - LocalDB comes with Visual Studio
   - Or install SQL Server Express

4. **Redis**
   - **Windows:** Use [Memurai](https://www.memurai.com/) or WSL2 with Redis
   - **Linux/Mac:** 
     ```bash
     # Install Redis
     sudo apt-get install redis-server  # Ubuntu/Debian
     brew install redis                  # macOS
     
     # Start Redis
     redis-server
     ```

### Backend Setup

1. **Navigate to Backend directory:**
   ```bash
   cd Backend/ChatSupportApi
   ```

2. **Update Connection Strings** (if needed):
   
   Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChatSupportDb;Trusted_Connection=True;",
       "Redis": "localhost:6379"
     }
   }
   ```

3. **Restore packages:**
   ```bash
   dotnet restore --configfile NuGet.config
   ```

4. **Initialize database:**
   ```bash
   dotnet ef database update
   # OR the app will auto-create on first run
   ```

5. **Run the API:**
   ```bash
   dotnet run
   ```

   API will be available at: `https://localhost:5001` or `http://localhost:5000`

### Frontend Setup

1. **Navigate to Frontend directory:**
   ```bash
   cd Frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Install SignalR client:**
   ```bash
   npm install @microsoft/signalr
   ```

4. **Update API URL** (if different):
   
   Edit `src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:5001/api',
     hubUrl: 'https://localhost:5001/chathub'
   };
   ```

5. **Run the application:**
   ```bash
   ng serve
   ```

   App will be available at: `http://localhost:4200`

## 📡 API Endpoints

### Chat Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat/request` | Create new chat session |
| GET | `/api/chat/session/{id}` | Get session details |
| POST | `/api/chat/session/{id}/poll` | Update session poll timestamp |
| POST | `/api/chat/session/{id}/complete` | Complete chat session |
| GET | `/api/chat/stats` | Get queue statistics |

### Agent Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/agent` | Get all agents |
| GET | `/api/agent/{id}` | Get specific agent |
| PUT | `/api/agent/{id}/shift-ending` | Set agent shift ending status |
| GET | `/api/agent/team/{teamId}` | Get team agents |

### Team Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/team` | Get all teams |
| GET | `/api/team/{id}` | Get specific team with agents |

### SignalR Hub

**Endpoint:** `/chathub`

**Methods:**
- `JoinChatSession(sessionId)` - Join a chat room
- `LeaveChatSession(sessionId)` - Leave a chat room
- `SendMessage(sessionId, senderId, senderName, message)` - Send message

**Events:**
- `ReceiveMessage` - Receive chat messages
- `AgentAssigned` - Notification when agent is assigned

## 🔧 Configuration

### Database Configuration

The system uses Entity Framework Core with SQL Server. Initial data is seeded automatically:

- 3 operational teams (A, B, C)
- 1 overflow team
- 16 agents with various seniority levels

### Redis Configuration

Redis is used for FIFO queue management. The queue key is `chat:queue`.

**Connection string format:**
```
localhost:6379
```

For production with authentication:
```
host:port,password=yourpassword,ssl=true
```

### Background Services

1. **Session Monitor Service** - Runs every 2 seconds
   - Monitors for inactive sessions
   - Removes inactive sessions from queue
   - Releases agents

2. **Queue Processor Service** - Runs every 3 seconds
   - Processes pending chat sessions
   - Assigns chats to available agents
   - Implements round-robin algorithm

## 📊 Assignment Algorithm Example

**Scenario:** Team with 2 Juniors (cap 4 each), 1 Mid (cap 6)

**Incoming:** 6 chats

**Assignment:**
1. Chat 1 → Junior 1 (1/4)
2. Chat 2 → Junior 2 (1/4)
3. Chat 3 → Junior 1 (2/4)
4. Chat 4 → Junior 2 (2/4)
5. Chat 5 → Junior 1 (3/4)
6. Chat 6 → Junior 2 (3/4)

Mid-level agent remains at 0/6, available to assist or handle escalations.

## 🧪 Testing

### Test Chat Request

```bash
curl -X POST https://localhost:5001/api/chat/request \
  -H "Content-Type: application/json" \
  -d '{"userId":"user123"}'
```

**Expected Response:**
```json
{
  "message": "OK",
  "sessionId": "guid-here",
  "status": "queued"
}
```

### Check Queue Stats

```bash
curl https://localhost:5001/api/chat/stats
```

**Expected Response:**
```json
{
  "currentCapacity": 51,
  "maxQueueSize": 76,
  "currentQueueLength": 0,
  "availableSlots": 76
}
```

## 🏢 Project Structure

```
chat-app/
├── Backend/
│   └── ChatSupportApi/
│       ├── Controllers/          # API endpoints
│       ├── Models/               # Domain entities
│       ├── Data/                 # DbContext and migrations
│       ├── Services/             # Business logic
│       ├── Hubs/                 # SignalR hubs
│       ├── BackgroundServices/   # Background workers
│       └── Program.cs            # Application startup
│
├── Frontend/
│   └── src/
│       ├── app/
│       │   ├── components/       # Angular components
│       │   ├── services/         # API and SignalR services
│       │   └── models/           # TypeScript interfaces
│       └── environments/         # Configuration
│
└── README.md
```

## 🔐 Security Considerations

For production deployment:

1. **Enable authentication** - Add JWT or OAuth2
2. **Secure Redis** - Enable password and TLS
3. **SQL Server** - Use Windows Authentication or secure credentials
4. **CORS policies** - Restrict to specific origins
5. **Rate limiting** - Prevent abuse
6. **Input validation** - Sanitize all inputs
7. **HTTPS only** - Force secure connections

## 🐛 Troubleshooting

### Redis Connection Issues

**Error:** "Connection refused"  
**Solution:** Ensure Redis is running: `redis-cli ping` should return `PONG`

### Database Connection Issues

**Error:** "Cannot open database"  
**Solution:** 
- Check SQL Server is running
- Verify connection string
- Run: `dotnet ef database update`

### CORS Errors

**Error:** "Access-Control-Allow-Origin"  
**Solution:** Ensure frontend URL matches CORS policy in `Program.cs`

### SignalR Connection Failed

**Error:** "Failed to start connection"  
**Solution:**
- Check backend is running
- Verify hub URL in Angular environment
- Check browser console for details

## 📝 License

This is a demonstration project for educational purposes.

## 👥 Contributing

This is a test implementation. For production use, consider:
- Adding comprehensive unit tests
- Implementing integration tests
- Adding monitoring and logging (e.g., Application Insights)
- Implementing proper error handling
- Adding API versioning
- Implementing caching strategies
- Adding performance optimizations

## 📞 Support

For issues or questions, please check the troubleshooting section or review the code documentation.
