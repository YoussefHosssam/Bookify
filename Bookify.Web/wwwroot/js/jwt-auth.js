// JWT Authentication Helper
const JwtAuth = {
    // Get token from localStorage or cookie
    getToken: function() {
        // Try localStorage first
        let token = localStorage.getItem('jwt_token');
        if (token) {
            return token;
        }
        
        // Try to get from cookie (for server-side rendered pages)
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'jwt_token') {
                return decodeURIComponent(value);
            }
        }
        
        return null;
    },
    
    // Store token in localStorage
    setToken: function(token) {
        localStorage.setItem('jwt_token', token);
    },
    
    // Remove token
    removeToken: function() {
        localStorage.removeItem('jwt_token');
        // Also try to remove cookie
        document.cookie = 'jwt_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
    },
    
    // Check if user is authenticated
    isAuthenticated: function() {
        return this.getToken() !== null;
    },
    
    // Decode JWT token (without verification - for client-side display only)
    decodeToken: function(token) {
        if (!token) return null;
        
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            
            return JSON.parse(jsonPayload);
        } catch (e) {
            console.error('Error decoding token:', e);
            return null;
        }
    },
    
    // Get user info from token
    getUserInfo: function() {
        const token = this.getToken();
        if (!token) return null;
        
        const decoded = this.decodeToken(token);
        if (!decoded) return null;
        
        return {
            userId: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.sub,
            email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email'] || decoded.email,
            roles: this.getRoles(decoded)
        };
    },
    
    // Get roles from decoded token
    getRoles: function(decoded) {
        const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
        if (decoded[roleClaim]) {
            return Array.isArray(decoded[roleClaim]) ? decoded[roleClaim] : [decoded[roleClaim]];
        }
        return [];
    },
    
    // Make authenticated API request
    fetch: async function(url, options = {}) {
        const token = this.getToken();
        
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await window.fetch(url, {
            ...options,
            headers: headers
        });
        
        // If unauthorized, remove token and redirect to login
        if (response.status === 401) {
            this.removeToken();
            window.location.href = '/Account/Login';
        }
        
        return response;
    },
    
    // Login via API
    login: async function(email, password, rememberMe = false) {
        try {
            const response = await window.fetch('/Account/Login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({ email, password, rememberMe }),
                credentials: 'include'
            });
            
            const result = await response.json();
            
            if (result.success && result.token) {
                this.setToken(result.token);
                return { success: true, data: result };
            } else {
                return { success: false, message: result.message || 'Login failed' };
            }
        } catch (error) {
            console.error('Login error:', error);
            return { success: false, message: 'Network error' };
        }
    },
    
    // Register via API
    register: async function(userName, email, password) {
        try {
            const response = await window.fetch('/Account/Register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({ userName, email, password }),
                credentials: 'include'
            });
            
            const result = await response.json();
            
            if (result.success && result.token) {
                this.setToken(result.token);
                return { success: true, data: result };
            } else {
                return { success: false, message: result.message || 'Registration failed', errors: result.errors };
            }
        } catch (error) {
            console.error('Registration error:', error);
            return { success: false, message: 'Network error' };
        }
    },
    
    // Logout
    logout: async function() {
        this.removeToken();
        
        // Call server-side logout
        try {
            await window.fetch('/Account/Logout', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
        } catch (error) {
            console.error('Logout error:', error);
        }
        
        window.location.href = '/';
    }
};

// Auto-attach token to fetch requests if available
const originalFetch = window.fetch;
window.fetch = function(url, options = {}) {
    const token = JwtAuth.getToken();
    if (token && options && typeof url === 'string' && url.startsWith('/')) {
        if (!options.headers) {
            options.headers = {};
        }
        if (!options.headers['Authorization']) {
            options.headers['Authorization'] = `Bearer ${token}`;
        }
    }
    return originalFetch.call(this, url, options);
};

