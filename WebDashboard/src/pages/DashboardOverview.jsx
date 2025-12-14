import React from 'react';
import { ArrowUpRight, MapPin, Database, Activity } from 'lucide-react';
import './DashboardOverview.css';

const DashboardOverview = () => {
    return (
        <div className="dashboard-overview">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h2 className="text-2xl font-bold">Admin Dashboard</h2>
                    <p className="text-muted">Manage building maps and room data.</p>
                </div>
                <button className="btn btn-primary">Sync Data</button>
            </div>

            <div className="grid grid-cols-3 gap-6 mb-8">
                <StatCard
                    title="Total Rooms"
                    value="45"
                    trend="Updated today"
                    icon={<MapPin size={24} />}
                    color="var(--primary)"
                />
                <StatCard
                    title="Target Data Size"
                    value="12 KB"
                    trend="Stable"
                    icon={<Database size={24} />}
                    color="var(--secondary)"
                />
                <StatCard
                    title="Floors Mapped"
                    value="5"
                    trend="All Active"
                    icon={<Activity size={24} />}
                    color="var(--accent)"
                />
            </div>

            <div className="grid grid-cols-3 gap-6">
                <div className="col-span-2 card">
                    <div className="flex items-center justify-between mb-6">
                        <h3 className="font-bold text-lg">Room Updates Activity</h3>
                    </div>
                    <div className="chart-placeholder flex items-end justify-between h-64 gap-4 px-4">
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
                    <h3 className="font-bold text-lg mb-6">Recent Changes</h3>
                    <div className="activity-list flex flex-col gap-4">
                        <ActivityItem
                            user="Admin"
                            action="Renamed Room 201 -> ICT Lab"
                            time="10m ago"
                        />
                        <ActivityItem
                            user="System"
                            action="Backup created"
                            time="1h ago"
                        />
                        <ActivityItem
                            user="Admin"
                            action="Updated Office of Dean Coords"
                            time="2h ago"
                        />
                        <ActivityItem
                            user="Admin"
                            action="Added 'Stock Room' to Floor 2"
                            time="1d ago"
                        />
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
