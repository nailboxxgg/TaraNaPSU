import { BrowserRouter as Router, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import LandingPage from './pages/LandingPage';
import LoginPage from './pages/LoginPage';
import DashboardLayout from './layouts/DashboardLayout';
import DashboardOverview from './pages/DashboardOverview';
import RoomManagement from './pages/RoomManagement';
import SettingsPage from './pages/SettingsPage';

const PrivateRoute = () => {
    const { isAuthenticated, isLoading } = useAuth();

    if (isLoading) return <div className="p-8 text-center">Loading...</div>;

    return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
};

function App() {
    return (
        <AuthProvider>
            <Router>
                <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/login" element={<LoginPage />} />

                    <Route element={<PrivateRoute />}>
                        <Route path="/dashboard" element={<DashboardLayout />}>
                            <Route index element={<DashboardOverview />} />
                            <Route path="rooms" element={<RoomManagement />} />
                            <Route path="settings" element={<SettingsPage />} />
                        </Route>
                    </Route>
                </Routes>
            </Router>
        </AuthProvider>
    );
}

export default App;
