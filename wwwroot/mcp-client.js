// MCP Client JavaScript

let authToken = localStorage.getItem('jwt_token') || '';
let baseUrl = window.location.origin;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    if (authToken) {
        document.getElementById('jwtToken').value = authToken;
        updateAuthStatus();
    }
    getServerStatus();
});

// Update authentication status
function updateAuthStatus() {
    const statusElement = document.getElementById('authStatus');
    if (authToken) {
        statusElement.textContent = 'Token Set';
        statusElement.className = 'status connected';
    } else {
        statusElement.textContent = 'Not Authenticated';
        statusElement.className = 'status disconnected';
    }
}

// Save JWT token
function saveToken() {
    const tokenInput = document.getElementById('jwtToken');
    authToken = tokenInput.value.trim();
    if (authToken) {
        localStorage.setItem('jwt_token', authToken);
    } else {
        localStorage.removeItem('jwt_token');
    }
    updateAuthStatus();
}

// Make authenticated API call
async function makeApiCall(endpoint, method = 'GET', body = null) {
    saveToken(); // Save current token before making call
    
    const headers = {
        'Content-Type': 'application/json'
    };
    
    if (authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
    }
    
    const config = {
        method: method,
        headers: headers
    };
    
    if (body && method !== 'GET') {
        config.body = typeof body === 'string' ? body : JSON.stringify(body);
    }
    
    try {
        const response = await fetch(`${baseUrl}${endpoint}`, config);
        const data = await response.json();
        
        return {
            success: response.ok,
            status: response.status,
            data: data,
            headers: Object.fromEntries(response.headers.entries())
        };
    } catch (error) {
        return {
            success: false,
            error: error.message
        };
    }
}

// Display response in element
function displayResponse(elementId, response) {
    const element = document.getElementById(elementId);
    
    if (response.success) {
        element.className = 'response success';
        element.textContent = JSON.stringify(response.data, null, 2);
    } else {
        element.className = 'response error';
        if (response.error) {
            element.textContent = `Error: ${response.error}`;
        } else {
            element.textContent = `HTTP ${response.status}\n${JSON.stringify(response.data, null, 2)}`;
        }
    }
}

// Authentication functions
async function testAuth() {
    const response = await makeApiCall('/api/auth/test');
    displayResponse('toolsResponse', response);
    
    if (response.success) {
        const statusElement = document.getElementById('authStatus');
        statusElement.textContent = 'Authenticated';
        statusElement.className = 'status connected';
    }
}

async function getUserInfo() {
    const response = await makeApiCall('/api/auth/userinfo');
    displayResponse('toolsResponse', response);
}

async function getServerStatus() {
    const response = await makeApiCall('/api/auth/status');
    displayResponse('statusResponse', response);
}

// User details functions
async function getUserDetails() {
    const response = await makeApiCall('/api/user/details');
    displayResponse('toolsResponse', response);
}

async function getUserSummary() {
    const response = await makeApiCall('/api/user/summary');
    displayResponse('toolsResponse', response);
}

async function getUserClaims() {
    const response = await makeApiCall('/api/user/claims');
    displayResponse('userResponse', response);
}

async function getApiKeyStatus() {
    const response = await makeApiCall('/api/userapikey/status');
    displayResponse('userResponse', response);
}

// MCP Tools functions
async function getTools() {
    const response = await makeApiCall('/api/mcp/tools');
    displayResponse('toolsResponse', response);
}

async function callTool(toolName, args = {}) {
    const response = await makeApiCall('/api/mcp/tools/call', 'POST', {
        name: toolName,
        arguments: args
    });
    displayResponse('toolsResponse', response);
}

// MCP Resources functions
async function getResources() {
    const response = await makeApiCall('/api/mcp/resources');
    displayResponse('resourcesResponse', response);
}

async function readResource(uri) {
    const response = await makeApiCall('/api/mcp/resources/read', 'POST', {
        uri: uri
    });
    displayResponse('resourcesResponse', response);
}

// Custom API call
async function makeCustomCall() {
    const url = document.getElementById('customUrl').value;
    const method = document.getElementById('httpMethod').value;
    const bodyText = document.getElementById('customBody').value;
    
    let body = null;
    if (bodyText.trim() && method !== 'GET') {
        try {
            body = JSON.parse(bodyText);
        } catch (e) {
            displayResponse('customResponse', {
                success: false,
                error: 'Invalid JSON in request body'
            });
            return;
        }
    }
    
    const response = await makeApiCall(url, method, body);
    displayResponse('customResponse', response);
}

// Weather tool with city input
async function callWeatherTool() {
    const city = prompt('Enter city name:', 'London');
    if (!city) return;
    
    const response = await makeApiCall('/api/mcp/tools/call', 'POST', {
        name: 'get_weather',
        arguments: { city: city }
    });
    displayResponse('toolsResponse', response);
}


// MCP Initialize (if needed)
async function initializeMcp() {
    const response = await makeApiCall('/api/mcp/initialize', 'POST', {
        protocolVersion: '2024-11-05',
        clientInfo: {
            name: 'Web Test Client',
            version: '1.0.0'
        }
    });
    displayResponse('toolsResponse', response);
}

// Helper functions
function clearResponses() {
    document.getElementById('toolsResponse').textContent = '';
    document.getElementById('resourcesResponse').textContent = '';
    document.getElementById('customResponse').textContent = '';
    document.getElementById('statusResponse').textContent = '';
}

function exportToken() {
    if (authToken) {
        navigator.clipboard.writeText(authToken).then(() => {
            alert('Token copied to clipboard');
        });
    }
}

// Claude AI Chat functions
async function chatWithClaude() {
    const message = document.getElementById('chatMessage').value.trim();
    if (!message) {
        alert('Please enter a message');
        return;
    }
    
    const response = await makeApiCall('/api/chat/completions', 'POST', {
        message: message,
        useTools: false
    });
    displayResponse('chatResponse', response);
}

async function chatWithClaudeTools() {
    const message = document.getElementById('chatMessage').value.trim();
    if (!message) {
        alert('Please enter a message');
        return;
    }
    
    const response = await makeApiCall('/api/chat/completions', 'POST', {
        message: message,
        useTools: true
    });
    displayResponse('chatResponse', response);
}

// Alternative: Use MCP tools to call Claude
async function callClaudeTool(toolName) {
    const message = document.getElementById('chatMessage').value.trim();
    if (!message) {
        alert('Please enter a message');
        return;
    }
    
    const args = toolName === 'chat_with_claude' 
        ? { message: message }
        : { message: message };
    
    const response = await makeApiCall('/api/mcp/tools/call', 'POST', {
        name: toolName,
        arguments: args
    });
    displayResponse('chatResponse', response);
}

// Add keyboard shortcuts
document.addEventListener('keydown', function(e) {
    if (e.ctrlKey && e.key === 'Enter') {
        const activeElement = document.activeElement;
        if (activeElement.id === 'jwtToken') {
            testAuth();
        } else if (activeElement.id === 'customUrl' || activeElement.id === 'customBody') {
            makeCustomCall();
        } else if (activeElement.id === 'chatMessage') {
            chatWithClaude();
        }
    }
});