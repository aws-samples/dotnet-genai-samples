<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MonteCarloControl.ascx.cs" Inherits="risk_calculator_webapp.Controls.MonteCarloControl" %>

<div class="monte-carlo-control">
    <div class="simulation-controls">
        <h4>Monte Carlo Risk Simulation</h4>
        
        <div class="form-group">
            <label for="txtSimulations">Number of Simulations:</label>
            <asp:TextBox ID="txtSimulations" runat="server" CssClass="form-control"></asp:TextBox>
            <asp:TextBox ID="txtTestChan" runat="server" />
            <asp:RequiredFieldValidator ID="rfvSimulations" runat="server" 
                ControlToValidate="txtSimulations" 
                ErrorMessage="Number of simulations is required"
                CssClass="error-message" />
            <asp:RegularExpressionValidator ID="revSimulations" runat="server" 
                ControlToValidate="txtSimulations" 
                ValidationExpression="^\d+$"
                ErrorMessage="Please enter a valid number"
                CssClass="error-message" />
            <asp:RangeValidator ID="rvSimulations" runat="server"
                ControlToValidate="txtSimulations"
                Type="Integer"
                MinimumValue="100"
                MaximumValue="10000"
                ErrorMessage="Simulations must be between 100 and 10,000"
                CssClass="error-message" />
        </div>
        
        <div class="form-group">
            <asp:Button ID="btnRunSimulation" runat="server" Text="Run Simulation" 
                CssClass="btn btn-primary" OnClick="btnRunSimulation_Click" />
            <asp:Button ID="btnClearHistory" runat="server" Text="Clear History" 
                CssClass="btn btn-secondary" OnClick="btnClearHistory_Click" />
        </div>
    </div>
    
    <asp:Panel ID="pnlResults" runat="server" CssClass="results-panel" Visible="false">
        <div class="simulation-info">
            <p>Running <asp:Label ID="lblSimulationCount" runat="server"></asp:Label> simulations for portfolio risk scenarios...</p>
        </div>
        
        <table class="results-table">
            <tr>
                <td>Simulations Run</td>
                <td><asp:Label ID="lblSimulationsRun" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>95% VaR (Daily)</td>
                <td><asp:Label ID="lblVaR95" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>99% VaR (Daily)</td>
                <td><asp:Label ID="lblVaR99" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>Expected Return</td>
                <td><asp:Label ID="lblExpectedReturn" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>Simulation Time</td>
                <td><asp:Label ID="lblSimulationTime" runat="server"></asp:Label></td>
            </tr>
        </table>
        
        <div class="chart-placeholder">
            <p><i>Chart visualization would be displayed here in a production application</i></p>
        </div>
    </asp:Panel>
    
    <div class="simulation-history">
        <h5>Recent Simulations</h5>
        <table id="tblHistory" class="display" style="width:100%">
            <thead>
                <tr>
                    <th>Time</th>
                    <th>Simulations</th>
                    <th>95% VaR</th>
                    <th>99% VaR</th>
                    <th>Expected Return</th>
                    <th>Time (ms)</th>
                </tr>
            </thead>
            <tbody id="tblHistoryBody" runat="server">
            </tbody>
        </table>
        <script>
            $(document).ready(function () {
                $('#tblHistory').DataTable({
                    paging: true,
                    searching: false,
                    ordering: true,
                    order: []
                });
            });
        </script>
    </div>
    
    <div class="error-panel">
        <asp:Label ID="lblError" runat="server" CssClass="error-message" Visible="false"></asp:Label>
    </div>
    
    <asp:HiddenField ID="hfSimulationHistory" runat="server" />
    <asp:HiddenField ID="hfLastSimulationCount" runat="server" />
</div>
