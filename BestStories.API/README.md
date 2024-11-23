# BestStories Solution

The `BestStories` solution consists of the following projects:

1. **BestStories.API**: The entry point for the application, exposing endpoints for accessing the best Hacker News stories.
2. **BestStories.Infrastructure**: Handles the integration with external services, including the Hacker News API, caching, and configuration management.
3. **BestStories.Application**: Contains the core business logic and application services for fetching, processing, and managing stories.
4. **BestStories.Domain**: Defines the domain models and interfaces central to the application's functionality.
5. **BestStories.Tests**: Contains unit tests for various components across the solution.

## Overview

The solution fetches and processes top stories from Hacker News, exposing them through a RESTful API. It leverages caching to improve performance and applies rate-limiting to protect the external API. The system adheres to clean architecture principles, separating concerns across projects.

## Projects

### BestStories.API

- Hosts the web application and provides RESTful API endpoints.
- Configures middleware for routing, rate-limiting, logging, and monitoring (e.g., Prometheus).
- Depends on services and interfaces defined in `BestStories.Application`.

### BestStories.Infrastructure

- Manages integrations with external systems, such as the Hacker News API.
- Implements caching mechanisms using `IMemoryCache`.
- Configures and applies external settings using `IConfiguration`.

### BestStories.Application

- Encapsulates the core application logic for fetching, processing, and filtering stories.
- Defines application services, including abstractions for interacting with infrastructure layers.
- Implements story-fetching strategies with optimal performance and memory allocation.

### BestStories.Domain

- Defines domain models like `BestHackerNewsStory` and interfaces for abstractions.
- Establishes the business rules and structure central to the application.
- Focuses on maintaining a clean and reusable domain layer.

### BestStories.Tests

- Contains unit tests for the `BestStories` solution.
- Uses xUnit for testing, along with Moq for mocking dependencies.
- Covers various scenarios to ensure the reliability and correctness of the application.

## Usage

1. Deploy the `BestStories.API` project to host the RESTful API.
2. Configure the application settings (e.g., API endpoints, caching policies) in `appsettings.json`.
3. Integrate Prometheus and Grafana for monitoring performance and usage metrics.
4. Run the `BestStories.Tests` project to validate the functionality of the solution.