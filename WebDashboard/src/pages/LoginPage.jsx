import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { User, Lock, ArrowRight, AlertCircle, Eye, EyeOff } from 'lucide-react';
import Logo from '../assets/logo.png';
import './LoginPage.css';

const LoginPage = () => {
    const [username, setUsername] = useState('admin');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const { login, isAuthenticated } = useAuth();
    const navigate = useNavigate();

    if (isAuthenticated) {
        return <Navigate to="/dashboard" replace />;
    }

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        await new Promise(r => setTimeout(r, 800)); // Fake network delay for smooth UX

        if (login(username, password)) {
            navigate('/dashboard');
        } else {
            setError('Invalid username or password.');
            setIsLoading(false);
        }
    };

    return (
        <div className="login-container">
            {/* Left Side - Visual */}
            <div className="login-visual">
                <div className="visual-content">
                    <h1 className="visual-title">Discover.<br />Manage.<br />Grow.</h1>
                    <p className="visual-subtitle">The official TaraNaPSU Admin Dashboard.</p>
                </div>
                <div className="visual-overlay"></div>
                <div className="visual-shapes">
                    <div className="shape s1"></div>
                    <div className="shape s2"></div>
                </div>
            </div>

            {/* Right Side - Form */}
            <div className="login-form-wrapper">
                <div className="login-form-content">
                    <div className="brand-header">
                        <img src={Logo} alt="TaraNaPSU" className="brand-logo" />
                        <span className="brand-name">TaraNaPSU</span>
                    </div>

                    <div className="form-header">
                        <h2>Sign In</h2>
                        <p>Welcome back! Please enter your admin details.</p>
                    </div>

                    <form onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label>Username</label>
                            <div className="input-has-icon">
                                <User size={18} className="input-icon" />
                                <input
                                    type="text"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    placeholder="Enter your username"
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label>Password</label>
                            <div className="input-has-icon">
                                <Lock size={18} className="input-icon" />
                                <input
                                    type={showPassword ? "text" : "password"}
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="Enter your password"
                                />
                                <button
                                    type="button"
                                    className="password-toggle"
                                    onClick={() => setShowPassword(!showPassword)}
                                >
                                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                                </button>
                            </div>
                        </div>

                        {error && (
                            <div className="error-message">
                                <AlertCircle size={16} /> {error}
                            </div>
                        )}

                        <button type="submit" className="btn btn-primary w-full login-btn" disabled={isLoading}>
                            {isLoading ? "Signing in..." : "Sign In"}
                        </button>
                    </form>


                </div>
            </div>
        </div>
    );
};

export default LoginPage;
