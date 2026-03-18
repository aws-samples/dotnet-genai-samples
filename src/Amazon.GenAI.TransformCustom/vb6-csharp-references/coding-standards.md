# Company C# Coding Standards

## Naming Conventions

- VB6 Hungarian notation (strCustomerName, intCount) converts to PascalCase (CustomerName, Count)
- Private fields use underscore prefix: _customerRepository
- Async methods must have Async suffix: GetCustomerAsync, SaveOrderAsync
- Interfaces use I prefix: ICustomerService, IOrderRepository

## Modern C# Features (Required)

- Use file-scoped namespaces in all files
- Use records for DTOs: public record CustomerDto(int Id, string Name, string Email);
- Use primary constructors for services: public class CustomerService(SksDbContext db, ILogger<CustomerService> logger)
- Use collection expressions: List<Customer> customers = [];
- Use pattern matching for conditionals instead of if/else chains
- Use target-typed new: Customer customer = new();
- Use null-coalescing assignment: _cache ??= LoadCache();

## Architecture Standards

- All services must be registered in DI container
- All database operations must be async
- All entity classes must implement IAuditable (CreatedDate, CreatedBy, ModifiedDate, ModifiedBy) 
- Use ILogger with structured logging for all error handling
- Include correlation ID in all log entries

## Project Structure

/Pages           - Blazor page components (one per VB6 form)
/Components      - Shared Blazor components
/Models          - Entity classes and DTOs (as records)
/Services        - Business logic services
/Data            - DbContext and repository classes
/Extensions      - Extension method classes organized by domain

## Company Business Rules for Transformation

### Customer Status Mapping

- VB6 uses integer status codes (1=Active, 2=Inactive, 3=Suspended) 
- Transform to strongly-typed enum with extension method for legacy compatibility 

### Date Handling Standards

- All date operations must use DateOnly for dates without time components 
- Legacy VB6 date strings in "MM/dd/yyyy" format must use DateTime.ParseExact 

### Audit Trail Requirements

- All entity classes must implement IAuditable interface
- Include CreatedDate, CreatedBy, ModifiedDate, ModifiedBy properties

### Error Handling Standards

- Wrap all database operations in try-catch with specific exception types
- Log exceptions using ILogger with structured logging
- Include correlation ID in all log entries
