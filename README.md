# Real-Time Mini Trading Platform

A full-stack, real-time trading dashboard built with ASP.NET Core and Angular. The platform integrates with a live market data provider via WebSocket, executing simulated market orders against live prices, and provides real-time position tracking and Mark-to-Market P/L calculations.

## 🚀 Technologies Used
* **Backend:** ASP.NET Core, C#, Entity Framework Core
* **Frontend:** Angular 18 (Standalone Components), RxJS, HTML/CSS
* **Database:** SQLite (Zero-configuration, fully portable)
* **Real-time Communication:** SignalR (Backend to Frontend), WebSockets (Backend to Provider)

## ✨ Key Features
* **Live Price Streaming:** Connects to the ActTrader WebSocket feed and pushes high-frequency updates to the UI via SignalR.
* **REST API Authentication:** Securely retrieves the provider's API token using HTTP Digest Authentication.
* **Throttled UI Updates:** Employs RxJS `bufferTime` to intelligently batch high-frequency SignalR updates, preventing browser UI freezing.
* **Order Execution Engine:** Processes market orders instantly against the latest cached bid/ask prices.
* **Real-Time P/L:** Calculates aggregate open positions and Mark-to-Market unrealized profit/loss on every tick.
* **Graceful Degradation (Simulated Feed):** Includes a built-in Random Walk simulated data generator if the live provider is unreachable or the market is closed.

---

## 🛠️ Setup Instructions

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 8+)
* [Node.js](https://nodejs.org/) (Version 18+)

### 1. Start the Backend
The backend utilizes SQLite. It requires zero database configuration and will automatically run Entity Framework migrations to build the database (`trading.db`) on startup.

```bash
cd backend/RealTimeTradingPlatform.Api
dotnet run
```
*The backend API will be available at `http://localhost:5050`.*
*(Swagger documentation is available at `http://localhost:5050/swagger`)*

### 2. Start the Frontend
Open a **new** terminal window and run:

```bash
cd frontend/real-time-trading
npm install
npm start
```
*The Angular application will compile and open at `http://localhost:4200`.*

---

## ⚙️ Configuration & Testing

### Live Market Data vs Simulated Data
If the live market is closed (e.g., weekends) or the demo provider feed is returning empty quotes, the UI will display exactly what the provider sends (`0.00`). 

To evaluate the application's responsiveness and P/L calculations during these times, you can force the application to use the built-in **Simulated Market Feed**:

1. Open `backend/RealTimeTradingPlatform.Api/appsettings.json`
2. Set `"ForceSimulatedData": true` inside the `TradingApi` section.
3. Restart the backend. 
4. The dashboard will immediately populate with actively fluctuating mock prices.

## 📐 Architecture Overview
1. **MarketDataBackgroundService:** A singleton background worker that manages the WebSocket connection, parses incoming ticks, and updates a thread-safe shared memory cache.
2. **SignalR Hub:** Broadcasts the latest market prices and newly executed trades to all connected browser clients.
3. **Data Access:** Entity Framework Core tracking `Orders` and `Trades` in a normalized schema.
4. **Services Layer:** Abstracted business logic (Authentication, Orders, Trades, Positions) decoupled from the API Controllers.
5. **Angular UI:** Employs RxJS observables for state management and dynamic UI rendering without page reloads.
