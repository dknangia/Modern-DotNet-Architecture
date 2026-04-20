# TaskFlow Distributed Outbox Pattern – Learning Project

This project demonstrates the Outbox pattern using a .NET Worker Service, RabbitMQ, and SQL Server. It is intended as a personal learning resource, but anyone is welcome to use or adapt it.

---

## Table of Contents

- [Overview](#overview)
- [Technologies Used](#technologies-used)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Setup & Running](#setup--running)
- [Usage](#usage)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

The Outbox pattern is used to ensure reliable message delivery between services by storing messages in a database table (the "outbox") and having a background worker publish them to a message broker (RabbitMQ). This project implements the pattern for educational purposes.

---

## Technologies Used

- .NET 10 Worker Service (C# 14)
- RabbitMQ (with Management UI)
- SQL Server 2022
- Docker & Docker Compose
- Entity Framework Core

---

## Architecture

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK installed
- Visual Studio 2026 (or later) recommended

---

## Setup & Running

### 1. Clone the Repository

### 2. Start Dependencies with Docker Compose

- SQL Server will be available on `localhost:1433` (SA password: `YourStrong@Pass123`)
- RabbitMQ will be available on `localhost:5672` (UI: http://localhost:15672, default user/pass: guest/guest)

### 3. Create the Database

Connect to SQL Server (e.g., with Azure Data Studio or `sqlcmd`) and create a database named `WorkTask`:

### 4. Apply Migrations (if using EF Core)

From the solution root:

### 5. Run the Worker Service

From the solution root:

---

## Usage

- Add tasks to the `OutboxMessages` table (manually or via API if available).
- The Worker Service will poll for unprocessed messages and publish them to RabbitMQ.
- Processed messages are marked with a timestamp.

---

## Troubleshooting

- **SQL Server connection issues:** Ensure Docker is running and port 1433 is not blocked.
- **RabbitMQ issues:** Access the management UI at [http://localhost:15672](http://localhost:15672) for diagnostics.
- **EF Core migration errors:** Check your connection string and ensure the database exists.

---

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

---

## License

This project is open source and available under the MIT License.
