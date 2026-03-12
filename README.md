# DevConnect API

DevConnect is a social media backend API built with **ASP.NET Core Web API**.  
The project provides authentication, posts, comments, likes, notifications, search, pagination and image upload functionalities similar to modern social media platforms.

---

# Features

- JWT Authentication
- User Register / Login
- User Profile
- Follow / Unfollow System
- Post Creation
- Comments
- Likes
- Personalized Feed
- Notifications
- Search
- Pagination
- Image Upload

---

# Technologies


- ASP.NET Core 8 Web API
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Swagger / OpenAPI
- Clean Architecture

---
## How to Run

1. Clone the repository

git clone https://github.com/BaranSivrikaya/devconnect-api.git

2. Navigate to project folder

cd devconnect-api

3. Run the project

dotnet run

4. Open Swagger

https://localhost:xxxx/swagger

# Project Structure

src/
├── DevConnect.API
├── DevConnect.Application
├── DevConnect.Domain
└── DevConnect.Infrastructure

---

# API Endpoints

## Auth

POST /api/Auth/register  
POST /api/Auth/login

## Users

GET /api/Users/{userId}  
GET /api/Users/me  
POST /api/Users/{userId}/follow  
DELETE /api/Users/{userId}/follow  
GET /api/Users/{userId}/followers  
GET /api/Users/{userId}/following  
GET /api/Users/search  

## Posts

POST /api/Posts  
GET /api/Posts  
POST /api/Posts/{postId}/like  
DELETE /api/Posts/{postId}/like  

## Comments

POST /api/Posts/{postId}/comments  
GET /api/Posts/{postId}/comments  

## Feed

GET /api/Feed

## Notifications

GET /api/Notifications  
PUT /api/Notifications/{id}/read  

## Uploads

POST /api/Uploads/image  

---

# Pagination Example
GET /api/Posts?page=1&pageSize=10


---

# Author

Baran Sivrikaya  
Computer Engineer
