import React, { useState, useEffect } from 'react';
import { ArrowUpRight, MapPin, Database, Activity, RefreshCw } from 'lucide-react';
import { db } from '../firebase/config';
import { collection, onSnapshot, query, orderBy, limit, getDocs } from 'firebase/firestore';
import './DashboardOverview.css';

const DashboardOverview = () => {
    const [stats, setStats] = useState({
        totalRooms: 0,
        floorsMapped: 0,
        updatesToday: 0
    });
    const [recentActivity, setRecentActivity] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Subscribe to real-time room updates
        const unsubscribe = onSnapshot(collection(db, "rooms"), (snapshot) => {
            const rooms = snapshot.docs.map(doc => doc.data());

            // Calculate Stats
            const uniqueFloors = new Set(rooms.map(r => r.FloorNumber)).size;

            setStats({
                totalRooms: rooms.length,
                floorsMapped: uniqueFloors || 0,
                targetDataSize: '18 KB' // Approximate static value or calc from JSON string length
            });

            // For this demo, we'll generate activity logs from the latest changes
            // In a real app, you'd have a separate "audit_logs" collection
            // Here we just show the most recently modified rooms if we had a timestamp, 
            // but since we don't, we'll just show the top 5 rooms as a placeholder for "Recent"
            setRecentActivity(rooms.slice(0, 5).map(room => ({
                user: "Admin",
                action: `Updated ${room.Name}`,
                time: "Just now"
            })));

            setLoading(false);
        });

        return () => unsubscribe();
    }, []);

    return (
        <div className="dashboard-overview">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h2 className="text-2xl font-bold">Admin Dashboard</h2>
                    <p className="text-muted">Manage building maps and room data.</p>
                </div>
                <div className="flex items-center gap-2 text-sm text-green-600 bg-green-50 px-3 py-1 rounded-full">
                    <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
                    Live Sync Active
                </div>
            </div>

            <div className="grid grid-cols-3 gap-6 mb-8">
                <StatCard
                    title="Total Rooms"
                    value={loading ? "..." : stats.totalRooms}
                    trend="From Firebase"
                    icon={<MapPin size={24} />}
                    color="var(--primary)"
                />
                <StatCard
                    title="Target Data Size"
                    value={stats.targetDataSize}
                    trend="Stable"
                    icon={<Database size={24} />}
                    color="var(--secondary)"
                />
                <StatCard
                    title="Floors Mapped"
                    value={loading ? "..." : stats.floorsMapped}
                    trend="Active"
                    icon={<Activity size={24} />}
                    color="var(--accent)"
                />
            </div>

            <div className="grid grid-cols-3 gap-6">
                <div className="col-span-2 card">
                    <div className="flex items-center justify-between mb-6">
                        <h3 className="font-bold text-lg">Live Room Data</h3>
                    </div>
                    {/* Visual Placeholder for Data Distribution */}
                    <div className="chart-placeholder flex items-end justify-between h-64 gap-4 px-4">
                        {/* Fake chart data for visual aesthetics */}
                        {[20, 45, 30, 60, 40, 25, 35, 50, 70, 45, 30, 55].map((h, i) => (
                            <div key={i} className="bar" style={{ height: `${h}%`, background: i === 11 ? 'var(--primary)' : '#e5e7eb' }}></div>
                        ))}
                    </div>
                    <div className="flex justify-between mt-4 text-sm text-muted">
                        <span>Jan</span><span>Feb</span><span>Mar</span><span>Apr</span><span>May</span><span>Jun</span>
                        <span>Jul</span><span>Aug</span><span>Sep</span><span>Oct</span><span>Nov</span><span>Dec</span>
                    </div>
                </div>

                <div className="col-span-1 card">
                    <h3 className="font-bold text-lg mb-6">Live Changes</h3>
                    <div className="activity-list flex flex-col gap-4">
                        {loading ? (
                            <p className="text-muted text-sm">Loading activity...</p>
                        ) : recentActivity.length > 0 ? (
                            recentActivity.map((item, index) => (
                                <ActivityItem
                                    key={index}
                                    user={item.user}
                                    action={item.action}
                                    time={item.time}
                                />
                            ))
                        ) : (
                            <p className="text-muted text-sm">No recent activity.</p>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

const StatCard = ({ title, value, trend, icon, color }) => (
    <div className="card stat-card flex flex-col justify-between">
        <div className="flex items-start justify-between">
            <div>
                <p className="text-muted text-sm font-medium mb-1">{title}</p>
                <h3 className="text-3xl font-bold">{value}</h3>
            </div>
            <div className="icon-box" style={{ background: `${color}20`, color: color }}>
                {icon}
            </div>
        </div>
        <div className="trend mt-4 text-sm font-medium text-green-500 flex items-center gap-1">
            <ArrowUpRight size={14} />
            {trend}
        </div>
    </div>
);

const ActivityItem = ({ user, action, time }) => (
    <div className="activity-item flex items-center gap-3 pb-3 border-b border-gray-100 last:border-0">
        <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-xs font-bold text-gray-500">
            {user.charAt(0)}
        </div>
        <div className="flex-1">
            <p className="text-sm font-medium text-main">{user}</p>
            <p className="text-xs text-muted">{action}</p>
        </div>
        <span className="text-xs text-muted">{time}</span>
    </div>
);

export default DashboardOverview;
