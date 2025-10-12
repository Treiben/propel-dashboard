import { useState, useEffect } from 'react';
import FeatureFlagManager from './FeatureFlagManager';
import Login from './Login';
import UserManagement from './UserManagement';
import { apiService } from './services/apiService';
import { config } from './config/environment';
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

    const isAdmin = user.role === 'Admin';

    return (
        <div className="min-h-screen bg-gray-50">
            <nav className="bg-white shadow">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between h-16">
                        <div className="flex">
                            <div className="flex-shrink-0 flex items-center">
                                <h1 className="text-xl font-bold text-gray-900">Propel Dashboard</h1>
                            </div>
                            <div className="ml-6 flex space-x-8">
                                <button
                                    onClick={() => setCurrentView('flags')}
                                    className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${currentView === 'flags'
                                            ? 'border-blue-500 text-gray-900'
                                            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                        }`}
                                >
                                    Feature Flags
                                </button>
                                {isAdmin && (
                                    <button
                                        onClick={() => setCurrentView('users')}
                                        className={`inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium ${currentView === 'users'
                                                ? 'border-blue-500 text-gray-900'
                                                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                                            }`}
                                    >
                                        Users
                                    </button>
                                )}
                            </div>
                        </div>
                        <div className="flex items-center space-x-4">
                            <span className="text-sm text-gray-700">
                                {user.username} ({user.role})
                            </span>
                            <button
                                onClick={handleLogout}
                                className="px-3 py-2 text-sm text-gray-700 hover:text-gray-900"
                            >
                                Logout
                            </button>
                        </div>
                    </div>
                </div>
            </nav>

            <main>
                {currentView === 'flags' && <FeatureFlagManager />}
                {currentView === 'users' && isAdmin && <UserManagement />}
            </main>
        </div>
    );
}

export default App;