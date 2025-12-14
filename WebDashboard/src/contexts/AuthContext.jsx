import React, { createContext, useContext, useState, useEffect } from 'react';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    // Default credentials
    const DEFAULT_PASSWORD = "psuaccarcapstone2025-2026";
    const ADMIN_USERNAME = "admin";

    useEffect(() => {
        // Check local storage on initial load
        const storedAuth = localStorage.getItem('isAdminAuthenticated');
        if (storedAuth === 'true') {
            setIsAuthenticated(true);
        }

        // Initialize password if not set
        if (!localStorage.getItem('adminPassword')) {
            localStorage.setItem('adminPassword', DEFAULT_PASSWORD);
        }

        setIsLoading(false);
    }, []);

    const login = (username, password) => {
        const currentPassword = localStorage.getItem('adminPassword') || DEFAULT_PASSWORD;

        if (username === ADMIN_USERNAME && password === currentPassword) {
            setIsAuthenticated(true);
            localStorage.setItem('isAdminAuthenticated', 'true');
            return true;
        }
        return false;
    };

    const logout = () => {
        setIsAuthenticated(false);
        localStorage.removeItem('isAdminAuthenticated');
    };

    const changePassword = (currentPwd, newPwd) => {
        const storedPwd = localStorage.getItem('adminPassword') || DEFAULT_PASSWORD;
        if (currentPwd !== storedPwd) {
            return { success: false, message: "Incorrect current password." };
        }

        localStorage.setItem('adminPassword', newPwd);
        return { success: true, message: "Password updated successfully." };
    };

    return (
        <AuthContext.Provider value={{ isAuthenticated, login, logout, changePassword, isLoading }}>
            {children}
        </AuthContext.Provider>
    );
};
