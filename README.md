# 📧 Enron Email Processing System

This project processes and indexes the **Enron email dataset** using a **microservices architecture**.  
It includes services for **cleaning, indexing, and searching emails** using **RabbitMQ, MongoDB, and Elasticsearch**.

---

## 🚀 **Microservices Overview**
This system consists of **5 main services** running as **Docker containers**:

| Container Name  | Image  | Purpose |
|----------------|--------|---------|
| **rabbitmq** | `rabbitmq:management` | Message broker for email processing (RabbitMQ) |
| **mongodb** | `mongo:latest` | NoSQL database for storing indexed emails |
| **elasticsearch** | `docker.elastic.co/elasticsearch/elasticsearch:7.10.2` | Full-text search engine for querying emails |
| **emailindexer** | Custom Build | Microservice that consumes RabbitMQ messages and stores emails in MongoDB |
| **searchapi** | Custom Build | Microservice that searches emails in MongoDB and Elasticsearch |

---

## 📦 **How to Run Everything**
### **🔹 1. Run All Containers Using Docker Compose**
If you have `docker-compose.yml`, start all services with:
```sh
docker-compose up --build
