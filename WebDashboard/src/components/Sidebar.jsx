import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { LayoutDashboard, BookOpen, Settings, LogOut } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import Logo from '../assets/logo.png';
import './Sidebar.css';

const Sidebar = () => {
    const { logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        if (window.confirm("Are you sure you want to log out?")) {
            logout();
            navigate('/login');
        }
    };

    return (
        <aside className="sidebar">
            <div className="sidebar-header">
                <div className="logo-container">
                    <img src={Logo} alt="Admin Logo" style={{ width: '40px', height: '40px', objectFit: 'contain' }} />
                    <h1 className="logo-text">Admin Panel</h1>
                </div>
            </div>

            <nav className="sidebar-nav">
                <NavLink to="/dashboard" end className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
                    <LayoutDashboard size={20} />
                    <span>Overview</span>
                </NavLink>
                <NavLink to="/dashboard/rooms" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
                    <BookOpen size={20} />
                    <span>Room Management</span>
                </NavLink>
                <div style={{ flex: 1 }}></div> {/* Spacer to push settings down if needed */}
                <NavLink to="/dashboard/settings" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
                    <Settings size={20} />
                    <span>Settings</span>
                </NavLink>
            </nav>

            <div className="sidebar-footer">
                <button onClick={handleLogout} className="nav-item logout-btn">
                    <LogOut size={20} />
                    <span>Logout</span>
                </button>
            </div>
        </aside>
    );
};

export default Sidebar;
