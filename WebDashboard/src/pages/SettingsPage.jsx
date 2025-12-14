import React, { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Shield, Key, Check } from 'lucide-react';

const SettingsPage = () => {
    const { changePassword } = useAuth();

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [message, setMessage] = useState(null);

    const handleUpdatePassword = (e) => {
        e.preventDefault();
        setMessage(null);

        if (newPassword.length < 6) {
            setMessage({ type: 'error', text: 'New password must be at least 6 characters.' });
            return;
        }

        if (newPassword !== confirmPassword) {
            setMessage({ type: 'error', text: 'New passwords do not match.' });
            return;
        }

        const result = changePassword(currentPassword, newPassword);

        if (result.success) {
            setMessage({ type: 'success', text: result.message });
            setCurrentPassword('');
            setNewPassword('');
            setConfirmPassword('');
        } else {
            setMessage({ type: 'error', text: result.message });
        }
    };

    return (
        <div className="p-4" style={{ maxWidth: '800px' }}>
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h2 className="text-2xl font-bold">Admin Settings</h2>
                    <p className="text-muted">Manage your account security and preferences.</p>
                </div>
            </div>

            <div className="grid grid-cols-1 gap-8">
                {/* Security Section */}
                <div className="card">
                    <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-100">
                        <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
                            <Shield size={20} />
                        </div>
                        <h3 className="font-bold text-lg">Security</h3>
                    </div>

                    <form onSubmit={handleUpdatePassword} className="max-w-md">
                        <h4 className="font-medium mb-4 flex items-center gap-2">
                            <Key size={16} className="text-muted" /> Change Password
                        </h4>

                        {message && (
                            <div className={`p-3 rounded-lg mb-4 text-sm ${message.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                                {message.text}
                            </div>
                        )}

                        <div className="flex flex-col gap-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
                                <input
                                    type="password"
                                    className="input"
                                    value={currentPassword}
                                    onChange={(e) => setCurrentPassword(e.target.value)}
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                                <input
                                    type="password"
                                    className="input"
                                    value={newPassword}
                                    onChange={(e) => setNewPassword(e.target.value)}
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Confirm New Password</label>
                                <input
                                    type="password"
                                    className="input"
                                    value={confirmPassword}
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    required
                                />
                            </div>

                            <div className="pt-2">
                                <button type="submit" className="btn btn-primary">
                                    Update Password
                                </button>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default SettingsPage;
