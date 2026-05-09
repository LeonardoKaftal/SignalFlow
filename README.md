# SignalFlow

this is the backend of SignalFlow, is a REST API + SignalR server for real-time chat communication.
It provides user authentication, conversation management, message handling, participant management, and live updates over a SignalR hub.

## Tech Stack

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- JWT authentication with refresh tokens
- SignalR for real-time chat updates

## Features

- User registration and login
- JWT-based authorization
- Refresh token rotation via secure HTTP-only cookie
- Conversation creation and management
- Participant management with admin roles
- Message CRUD operations
- Real-time notifications through SignalR

## Base URLs

- REST API: `/api`
- SignalR hub: `/hubs/chat`

## Getting Started with Docker

The application can be run entirely via Docker Compose. A `compose.yaml` file is included in the project root.

### Prerequisites

- Docker
- Docker Compose

### Running with Docker Compose

Navigate to the `SignalFlowBackend/` folder and run:

```bash
sudo docker compose up .
```

This will:

1. **Build** the backend from the Dockerfile (ASP.NET Core 10.0)
2. **Start** PostgreSQL 16 Alpine container with health checks
3. **Start** PgAdmin 4 for database management (optional)
4. **Wait** for PostgreSQL to be ready before starting the app

### Service URLs

Once running, you can access:

- **Backend API**: `http://localhost:8080`
- **OpenAPI/Scalar Docs** (dev): `http://localhost:8080/scalar/` (if in development mode)
- **PgAdmin**: `http://localhost:5050`
  - Email: `admin@signalflow.com`
  - Password: `admin`

### Database Credentials (Development)

- **Host**: `postgres` (or `localhost` if accessing from host)
- **Port**: `5432`
- **Database**: `signalflow`
- **Username**: `signalflow`
- **Password**: `signalflow123`


## SignalR Hub

Hub route: `/hubs/chat`

The hub is protected by JWT authorization.

### Client Methods

#### `JoinConversation(conversationId)`
Join a conversation group.

Returns:
- `true` if the user is a valid participant and was added to the group
- `false` otherwise

#### `ExitConversation(conversationId)`
Leave a conversation group.

#### `UpdateLastMessageRead(conversationId)`
Marks the latest message in the conversation as read for the current user.

### Server-to-Client Events

The backend emits these hub events:

#### `MessageReceived`
Sent to the conversation group when a new message is saved.

#### `CreatedConversation`
Sent to specific users when a conversation is created.

#### `ParticipantAdded`
Sent to the conversation group when a participant is added.

#### `JoinConversation`
Sent to the user who has just been added, instructing them to join the SignalR group.

#### `PromotedAdmin`
Sent to the conversation group when a participant becomes admin.

#### `ParticipantRemoved`
Sent to the conversation group when a participant is removed.

#### `ConversationDeleted`
Sent to the conversation group when a conversation is deleted.

---


## Limitations / Known Gaps

This backend is functional, but there are a few important limitations to be aware of:

- **No paging**
  - List endpoints return full collections.
  - This can become expensive for large conversations or users with many messages.

- **No rate limiting**
  - There is no built-in request throttling.
  - The API can be abused by sending too many requests in a short time.

- **No anti-spam / abuse protection for messages**
  - There is no message spam detection.
  - Users can potentially flood conversations with repeated or excessive messages.

- **No advanced moderation controls**
  - There is no profanity filtering, content moderation, or automatic abuse handling.

---

## Notes

- OpenAPI / Scalar docs are available in development mode.
- JWT tokens are validated using issuer, audience, and signing key from configuration.

---
