import React, { useState, useEffect } from 'react';
import { Search, Edit2, Check, X, MapPin, Database, Loader } from 'lucide-react';
import { db } from '../firebase/config';
import { collection, onSnapshot, doc, updateDoc, writeBatch } from 'firebase/firestore';
import { TARGET_DATA } from '../data/targetData';

const RoomManagement = () => {
    const [rooms, setRooms] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState("");
    const [editingId, setEditingId] = useState(null);
    const [editValue, setEditValue] = useState("");
    const [seeding, setSeeding] = useState(false);

    // Subscribe to real-time updates from Firestore
    useEffect(() => {
        const unsubscribe = onSnapshot(collection(db, "rooms"), (snapshot) => {
            const roomList = snapshot.docs.map(doc => ({
                id: doc.id,
                ...doc.data()
            }));
            // Sort by FloorNumber then Name
            roomList.sort((a, b) => a.FloorNumber - b.FloorNumber || a.Name.localeCompare(b.Name));
            setRooms(roomList);
            setLoading(false);
        }, (error) => {
            console.error("Error fetching rooms:", error);
            setLoading(false);
        });

        return () => unsubscribe();
    }, []);

    const handleEditClick = (room) => {
        setEditingId(room.id);
        setEditValue(room.Name);
    };

    const handleSave = async (room) => {
        if (!editValue.trim() || editValue === room.Name) {
            setEditingId(null);
            return;
        }

        try {
            const roomRef = doc(db, "rooms", room.id);
            await updateDoc(roomRef, {
                Name: editValue
            });
            setEditingId(null);
        } catch (error) {
            console.error("Error updating room:", error);
            alert("Failed to update room name.");
        }
    };

    const handleCancel = () => {
        setEditingId(null);
    };

    const handleSeedDatabase = async () => {
        if (!confirm("This will upload all initial data from TargetData.json to Firestore. Continue?")) return;

        setSeeding(true);
        try {
            const batch = writeBatch(db);
            TARGET_DATA.TargetList.forEach((room) => {
                // Create a new document reference for each room
                const newRoomRef = doc(collection(db, "rooms"));
                batch.set(newRoomRef, room);
            });

            await batch.commit();
            alert("Database successfully populated!");
        } catch (error) {
            console.error("Error seeding database:", error);
            alert("Error uploading data: " + error.message);
        } finally {
            setSeeding(false);
        }
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
                {!loading && rooms.length === 0 && (
                    <button
                        onClick={handleSeedDatabase}
                        disabled={seeding}
                        className="btn btn-accent flex items-center gap-2"
                    >
                        {seeding ? <Loader className="animate-spin" size={18} /> : <Database size={18} />}
                        Initialize Database
                    </button>
                )}
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
                {loading ? (
                    <div className="p-8 text-center text-muted">Loading rooms...</div>
                ) : (
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
                                                onKeyDown={(e) => {
                                                    if (e.key === 'Enter') handleSave(room);
                                                    if (e.key === 'Escape') handleCancel();
                                                }}
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
                                                <button onClick={() => handleSave(room)} className="p-2 text-green-600 hover:bg-green-50 rounded-full">
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
                )}

                {!loading && filteredRooms.length === 0 && (
                    <div className="p-8 text-center text-muted">
                        {rooms.length === 0 ? "Database is empty. Click 'Initialize Database' to upload defaults." : "No rooms found matching your search."}
                    </div>
                )}
            </div>
        </div>
    );
};

export default RoomManagement;
