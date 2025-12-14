import React from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, BookOpen, Settings, LogOut, Shield } from 'lucide-react';
import './Sidebar.css';

const Sidebar = () => {
    return (
        <aside className="sidebar">
            <div className="sidebar-header">
                <div className="logo-container">
                    <div className="logo-icon"><Shield size={24} /></div>
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
                <button className="nav-item logout-btn">
                    <LogOut size={20} />
                    <span>Logout</span>
                </button>
            </div>
        </aside>
    );
};

export default Sidebar;
