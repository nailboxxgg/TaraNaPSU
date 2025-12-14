import React, { useState } from 'react';
import { Search, Edit2, Check, X, MapPin } from 'lucide-react';

// Mock Data simulating TargetData.json
const INITIAL_DATA = [
    { id: 1, Name: "B1-Quality Assurance Office", FloorNumber: 0 },
    { id: 2, Name: "B1-Extension Services", FloorNumber: 0 },
    { id: 3, Name: "B1-Office 1", FloorNumber: 0 },
    { id: 4, Name: "B1-Administrative Supply Office", FloorNumber: 0 },
    { id: 5, Name: "B1-Registrar's Office", FloorNumber: 0 },
    { id: 6, Name: "B1-Office of the CED", FloorNumber: 0 },
    { id: 7, Name: "B1-ICT-MO", FloorNumber: 1 },
    { id: 8, Name: "B1-CMT-Office", FloorNumber: 1 },
    { id: 9, Name: "B1-HTM-Office", FloorNumber: 1 },
    { id: 10, Name: "B2-NB 1", FloorNumber: 2 },
    { id: 11, Name: "B2-Clinic", FloorNumber: 2 },
    { id: 12, Name: "B3-Student Center", FloorNumber: 4 },
];

const RoomManagement = () => {
    const [rooms, setRooms] = useState(INITIAL_DATA);
    const [searchTerm, setSearchTerm] = useState("");
    const [editingId, setEditingId] = useState(null);
    const [editValue, setEditValue] = useState("");

    const handleEditClick = (room) => {
        setEditingId(room.id);
        setEditValue(room.Name);
    };

    const handleSave = () => {
        setRooms(rooms.map(room =>
            room.id === editingId ? { ...room, Name: editValue } : room
        ));
        setEditingId(null);
    };

    const handleCancel = () => {
        setEditingId(null);
    };

    const filteredRooms = rooms.filter(room =>
        room.Name.toLowerCase().includes(searchTerm.toLowerCase())
    );

    return (
        <div className="room-management">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h2 className="text-2xl font-bold">Room Management</h2>
                    <p className="text-muted">Rename offices and classrooms as they change.</p>
                </div>
            </div>

            <div className="card mb-6">
                <div className="flex items-center gap-4 px-4 py-3 bg-gray-50 rounded-lg border border-gray-100">
                    <Search size={20} className="text-gray-400" />
                    <input
                        type="text"
                        placeholder="Search for a room or office..."
                        className="bg-transparent border-none outline-none flex-1 text-sm font-medium"
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                    />
                </div>
            </div>

            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                <table className="w-full text-left border-collapse">
                    <thead>
                        <tr className="bg-gray-50 border-b border-gray-100 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                            <th className="px-6 py-4">Current Name</th>
                            <th className="px-6 py-4">Floor / Level</th>
                            <th className="px-6 py-4 text-right">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {filteredRooms.map(room => (
                            <tr key={room.id} className="hover:bg-gray-50 transition-colors">
                                <td className="px-6 py-4">
                                    {editingId === room.id ? (
                                        <input
                                            type="text"
                                            value={editValue}
                                            onChange={(e) => setEditValue(e.target.value)}
                                            className="input py-1 px-2 text-sm"
                                            autoFocus
                                        />
                                    ) : (
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-full bg-blue-50 text-blue-500 flex items-center justify-center">
                                                <MapPin size={16} />
                                            </div>
                                            <span className="font-medium text-gray-900">{room.Name}</span>
                                        </div>
                                    )}
                                </td>
                                <td className="px-6 py-4 text-sm text-gray-600">
                                    {room.FloorNumber === 0 ? 'Ground Floor' : `${room.FloorNumber}th Floor`}
                                </td>
                                <td className="px-6 py-4 text-right">
                                    {editingId === room.id ? (
                                        <div className="flex items-center justify-end gap-2">
                                            <button onClick={handleSave} className="p-2 text-green-600 hover:bg-green-50 rounded-full">
                                                <Check size={18} />
                                            </button>
                                            <button onClick={handleCancel} className="p-2 text-red-500 hover:bg-red-50 rounded-full">
                                                <X size={18} />
                                            </button>
                                        </div>
                                    ) : (
                                        <button onClick={() => handleEditClick(room)} className="text-primary hover:text-blue-800 font-medium text-sm flex items-center gap-1 ml-auto">
                                            <Edit2 size={16} /> Edit
                                        </button>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>

                {filteredRooms.length === 0 && (
                    <div className="p-8 text-center text-muted">
                        No rooms found.
                    </div>
                )}
            </div>
        </div>
    );
};

export default RoomManagement;
