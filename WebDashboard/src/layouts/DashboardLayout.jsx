import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import { Bell, Search, Menu } from 'lucide-react';
import './DashboardLayout.css';

const DashboardLayout = () => {
    const [sidebarOpen, setSidebarOpen] = useState(false);

    return (
        <div className="dashboard-layout">
            <Sidebar />
            <div className={`main-content ${sidebarOpen ? 'shifted' : ''}`}>
                <header className="topbar glass">
                    <div className="flex items-center gap-4">
                        {/* Mobile toggle would go here */}
                        <div className="search-bar">
                            <Search size={18} className="search-icon" />
                            <input type="text" placeholder="Search..." />
                        </div>
                    </div>

                    <div className="flex items-center gap-6">
                        <button className="icon-btn">
                            <Bell size={20} />
                            <span className="badge">3</span>
                        </button>
                        <div className="user-profile">
                            <div className="avatar">A</div>
                            <div className="user-info">
                                <span className="name">Admin User</span>
                                <span className="role">Super Admin</span>
                            </div>
                        </div>
                    </div>
                </header>

                <main className="page-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
};

export default DashboardLayout;
