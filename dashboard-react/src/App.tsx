import { useState, useEffect } from 'react';
import FeatureFlagManager from './FeatureFlagManager';
import Login from './Login';
import UserManagement from './UserManagement';
import { apiService } from './services/apiService';
import { config } from './config/environment';
import { Header } from './components/Header';
import './index.css';

interface UserInfo {
    username: string;
    role: string;
}

function App() {
    const [user, setUser] = useState<UserInfo | null>(null);
    const [loading, setLoading] = useState(true);
    const [currentView, setCurrentView] = useState<'flags' | 'users'>('flags');

    useEffect(() => {
        const token = localStorage.getItem(config.JWT_STORAGE_KEY);
        const userStr = localStorage.getItem('propel-user');

        if (token && userStr) {
            apiService.auth.setToken(token);
            setUser(JSON.parse(userStr));
        }

        setLoading(false);
    }, []);

    const handleLoginSuccess = (userInfo: UserInfo) => {
        setUser(userInfo);
    };

    const handleLogout = () => {
        localStorage.removeItem(config.JWT_STORAGE_KEY);
        localStorage.removeItem('propel-user');
        apiService.auth.clearToken();
        setUser(null);
        setCurrentView('flags');
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
                    <p className="text-gray-600">Loading...</p>
                </div>
            </div>
        );
    }

    if (!user) {
        return <Login onLoginSuccess={handleLoginSuccess} />;
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <Header 
                user={user}
                currentView={currentView}
                onViewChange={setCurrentView}
                onLogout={handleLogout}
            />

            <main>
                {currentView === 'flags' && <FeatureFlagManager />}
                {currentView === 'users' && user.role === 'Admin' && <UserManagement />}
            </main>
        </div>
    );
}

export default App;