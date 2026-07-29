# Risk Calculator - ASP.NET Web Forms Application

This project is a sample asp.net webform application features including ASCX user controls, ViewState/SessionState management, and comprehensive unit testing.

## Project Overview

The application is a financial risk management system that provide:
- Value at Risk (VaR) calculations
- Portfolio analysis
- Monte Carlo simulations
- Risk alert management

This ASP.NET Web Forms version provides the same functionality with a modern, responsive web interface, enhanced state management, and comprehensive testing.

## 🆕 New Features in v2.1

### ASCX User Controls
- **`NavigationControl.ascx`**: Reusable navigation component
- **`VarCalculationControl.ascx`**: VaR calculation with ViewState persistence
- **`PortfolioGridControl.ascx`**: Portfolio display with enhanced features
- **`MonteCarloControl.ascx`**: Monte Carlo simulation with history tracking

### Enhanced State Management
- **ViewState**: Form data persistence across postbacks
- **SessionState**: Cross-page data sharing and user session tracking
- **Event-driven architecture**: Controls communicate via events

### Comprehensive Unit Testing
- **Business Logic Tests**: RiskCalculator, MonteCarloService, RiskAlertService
- **State Management Tests**: ViewState and SessionState validation
- **Integration Tests**: End-to-end workflow testing
- **Mock Framework**: Moq for dependency injection testing

## Features

### 1. VaR Calculation (`CalculateVar.aspx`)
- Calculate Value at Risk for individual stocks
- Credit risk scoring
- Risk level categorization (Low/Medium/High)
- Form validation and error handling
- **NEW**: ViewState persistence and calculation history
- **NEW**: Session statistics tracking

### 2. Portfolio View (`Portfolio.aspx`)
- Display current portfolio holdings
- Calculate individual and total VaR
- Portfolio risk analysis and summary
- **NEW**: Enhanced insights and risk analysis
- **NEW**: Session-based view tracking

### 3. Monte Carlo Simulation (`MonteCarlo.aspx`)
- Run risk simulations with configurable iterations
- Calculate 95% and 99% VaR percentiles
- Expected return calculations
- Box-Muller transform for normal distribution
- **NEW**: Simulation history and performance tracking
- **NEW**: Session statistics and timing analysis

### 4. Risk Alerts (`RiskAlerts.aspx`)
- View and manage risk alerts
- Session-based alert storage
- Clear alerts functionality
- **NEW**: Enhanced alert management with timestamps

## Technical Architecture

### ASCX User Controls (`Controls/`)
- **`NavigationControl.ascx`**: Centralized navigation
- **`VarCalculationControl.ascx`**: VaR calculation with state management
- **`PortfolioGridControl.ascx`**: Portfolio display with insights
- **`MonteCarloControl.ascx`**: Simulation engine with history

### Business Logic Classes (`App_Code/`)
- **`RiskCalculator.cs`**: Core risk calculation logic
- **`MonteCarloService.cs`**: Monte Carlo simulation engine
- **`RiskAlertService.cs`**: Alert management service

### State Management
- **ViewState**: Form field persistence, calculation history, control state
- **SessionState**: User preferences, cross-page data, session statistics
- **Events**: Inter-control communication and parent page notifications

### Unit Testing (`RiskCalculatorWebForm.Tests/`)
- **Business Logic Tests**: Core functionality validation
- **State Management Tests**: ViewState and SessionState verification
- **Integration Tests**: End-to-end workflow testing
- **Mock Utilities**: Test data builders and HTTP context mocking

## File Structure

```
├── Default.aspx                 # Main navigation page
├── CalculateVar.aspx           # VaR calculation page
├── Portfolio.aspx              # Portfolio view page
├── MonteCarlo.aspx             # Monte Carlo simulation page
├── RiskAlerts.aspx             # Risk alerts page
├── web.config                  # Application configuration
├── Global.asax                 # Global application events
├── Content/
│   └── Site.css               # Enhanced CSS styling
├── Controls/                   # 🆕 ASCX User Controls
│   ├── NavigationControl.ascx
│   ├── VarCalculationControl.ascx
│   ├── PortfolioGridControl.ascx
│   └── MonteCarloControl.ascx
├── App_Code/
│   ├── RiskCalculator.cs      # Core business logic
│   ├── MonteCarloService.cs   # Simulation service
│   └── RiskAlertService.cs    # Alert management
├── RiskCalculatorWebForm.Tests/  # 🆕 Unit Test Project
│   ├── BusinessLogic/         # Business logic tests
│   ├── StateManagement/       # State management tests
│   ├── Integration/           # End-to-end tests
│   └── TestUtilities/         # Test helpers and mocks
├── Properties/
│   └── AssemblyInfo.cs        # Assembly information
└── *.designer.cs              # Auto-generated designer files
```

## Setup and Running

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.6.2
- IIS Express (included with Visual Studio)

### Running the Application
1. Open `RiskCalculatorWebForm.sln` in Visual Studio
2. Restore NuGet packages if prompted
3. Set `risk-calculator-webapp` as the startup project
4. Press F5 or click "Start Debugging"
5. The application will open in your default browser

### Running Unit Tests
1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Run all tests or specific test categories
4. View test results and coverage

### Default Portfolio Data
The application includes sample portfolio data:
- AAPL: $150,000
- GOOGL: $200,000
- MSFT: $175,000
- AMZN: $180,000
- TSLA: $120,000

## State Management Features

### ViewState Usage
- **Form Persistence**: Form values maintained across postbacks
- **Calculation History**: VaR calculation results stored per control
- **Control State**: UI state and user interactions preserved
- **Performance**: Optimized serialization for large datasets

### SessionState Usage
- **User Preferences**: Default values and settings
- **Cross-page Data**: Portfolio data and calculation results
- **Session Statistics**: Usage tracking and analytics
- **Alert Management**: Risk alerts persisted across pages

## Testing Strategy

### Unit Tests
- **Business Logic**: RiskCalculator, MonteCarloService, RiskAlertService
- **State Management**: ViewState and SessionState validation
- **Control Logic**: ASCX control functionality
- **Data Validation**: Input validation and edge cases

### Integration Tests
- **End-to-End Workflows**: Complete user scenarios
- **Cross-Component Communication**: Event handling and data flow
- **Performance Testing**: Simulation performance and memory usage
- **Data Consistency**: State synchronization across components

### Test Utilities
- **MockHttpContext**: HTTP context simulation for testing
- **TestDataBuilder**: Consistent test data generation
- **Assertion Helpers**: Custom validation methods

## Key Improvements over Python Version

### 1. Modern Architecture
- **Python**: CGI-based with global variables
- **ASP.NET**: Component-based with proper state management

### 2. State Management
- **Python**: Global dictionaries and lists (not thread-safe)
- **ASP.NET**: ViewState for form persistence, SessionState for user data

### 3. Code Organization
- **Python**: Single file with mixed concerns
- **ASP.NET**: Separated business logic, user controls, and presentation

### 4. Testing
- **Python**: No testing framework
- **ASP.NET**: Comprehensive unit testing with mocking

### 5. User Experience
- **Python**: Basic HTML forms
- **ASP.NET**: Rich server controls with validation and state persistence

## Browser Compatibility
- Chrome (recommended)
- Firefox
- Edge
- Safari
- Mobile browsers (responsive design)

## Performance Considerations
- ViewState optimization for large datasets
- SessionState cleanup and timeout management
- Monte Carlo simulation performance limits
- Memory management for long-running sessions

## Future Enhancements
- Database integration for persistent data
- Real-time market data integration
- Advanced charting with Chart.js or similar
- User authentication and authorization
- API endpoints for external integration
- Automated testing in CI/CD pipeline
- Logging and monitoring framework
- Performance profiling and optimization

## Development Guidelines
- Follow ASP.NET Web Forms best practices
- Maintain separation of concerns
- Use ViewState judiciously to avoid performance issues
- Implement proper error handling and validation
- Write comprehensive unit tests for new features
- Document state management patterns
