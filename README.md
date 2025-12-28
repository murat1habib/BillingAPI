💳 AI-Assisted Billing System (Local Setup)

This project is a local, AI-assisted billing system that allows users to query and pay bills through a chat-based interface.

It demonstrates how a Large Language Model (LLM) can be integrated into a real backend system to understand user intent, route requests, and trigger real API calls — all running locally.

🚀 What This Project Does

Users can interact with a billing system using natural language:

Ask for their bill

Request a detailed bill breakdown

See payment status

Pay bills using a Pay Now button

All interactions happen through a chat UI, backed by a real API, without exposing backend endpoints directly.

🧱 System Architecture (Local)
[ React Chat UI ]
        ↓ (Firestore)
[ Python LLM Agent ]
        ↓ (HTTP)
[ Node.js Gateway ]
        ↓ (JWT)
[ ASP.NET Core Billing API ]
        ↓
[ Local Database ]

🧩 Components
1️⃣ Billing API (ASP.NET Core 8)

Core business logic

Bill management

Payment simulation

Role-based authorization

2️⃣ Gateway Service (Node.js)

Acts as a secure proxy

Injects JWT tokens automatically

Separates client access from backend API

3️⃣ LLM Agent (Python + Ollama)

Uses a local LLM (Ollama)

Extracts intent & slots from user messages

Produces strict JSON output

Decides which backend endpoint to call

4️⃣ Chat UI (React + Firebase)

Real-time messaging with Firestore

Displays assistant responses

Shows Pay Now button only when allowed

No page refresh required

🤖 LLM Capabilities

The LLM is used only for intent understanding, not for business logic.

Supported intents:

query_bill

query_bill_detailed

pay_bill

help

Example user messages:

“Show my bill”

“Show detailed bill”

“Is my bill paid?”

The LLM always returns valid JSON only, which is then validated and executed safely.

🔐 Authentication & Roles

JWT-based authentication with role separation:

Role	Description
Admin	Manages bills (API-level)
Mobile	Queries bills
Banking	Processes payments

Tokens are handled internally by the Gateway.

📱 Chat-Based Flow

User sends a message

Message is saved to Firestore

Python agent listens for new messages

LLM extracts intent and parameters

Agent calls Gateway

Gateway calls Billing API

Response is sent back to chat

UI updates in real time

🛠 Technologies Used
Layer	Technology
Backend API	ASP.NET Core 8
ORM	Entity Framework Core
Database	Local SQL Database
Authentication	JWT Bearer Tokens
Gateway	Node.js (Express)
AI Agent	Python
LLM Runtime	Ollama
LLM Model	LLaMA 3.1
Frontend	React
Realtime DB	Firebase Firestore
API Docs	Swagger
📦 Project Structure
Billing.Api/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── AdminController.cs
│   ├── MobileController.cs
│   └── BankingController.cs
│
├── Data/
│   ├── BillingDbContext.cs
│   └── SeedData.cs
│
├── Models/
├── Dtos/
├── Middleware/
│
gateway-node/
agent-python/
frontend-react/

▶️ Running the Project Locally
1️⃣ Start Billing API
dotnet run

2️⃣ Start Gateway
cd gateway-node
npm install
npm start

3️⃣ Start LLM (Ollama)
ollama run llama3.1

4️⃣ Start LLM Agent
cd agent-python
python main.py

5️⃣ Start Frontend
cd frontend-react
npm install
npm start

🧪 Testing

Chat UI is the main interaction point

Swagger is available for backend inspection

All requests flow through the Gateway and Agent

🔒 Security Notes

No secrets are committed to GitHub

.env and service account files are ignored

All communication is local

🎥 Demo Video

The demo video shows:

Chat-based bill queries

LLM intent extraction

Bill detail vs summary

Payment flow with Pay Now button

(Source code is intentionally not shown in the video.)

✅ Current Status

✔ Fully local
✔ Stable LLM integration
✔ Chat-based UX complete
✔ Ready for demo & submission

📌 Final Note

This project focuses on practical LLM usage, not just AI text generation — demonstrating how LLMs can safely drive real backend workflows.
