import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
import DashboardLayout from './layouts/DashboardLayout';
import DashboardOverview from './pages/DashboardOverview';
import RoomManagement from './pages/RoomManagement';

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<LandingPage />} />
                <Route path="/dashboard" element={<DashboardLayout />}>
                    <Route index element={<DashboardOverview />} />
                    <Route path="rooms" element={<RoomManagement />} />
                    <Route path="settings" element={<div className="p-8">Admin Settings (Coming Soon)</div>} />
                </Route>
            </Routes>
        </Router>
    );
}

export default App;
