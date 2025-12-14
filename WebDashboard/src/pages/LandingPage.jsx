import React from 'react';
import { NavLink } from 'react-router-dom';
import { Download, Smartphone, Map, Navigation } from 'lucide-react';
import Logo from '../assets/logo.png';
import './LandingPage.css';

const LandingPage = () => {
    return (
        <div className="landing-page">
            <nav className="landing-nav glass">
                <div className="container flex items-center justify-between">
                    <div className="logo-container flex items-center gap-3">
                        <img src={Logo} alt="TaraNaPSU Logo" className="nav-logo" />
                        <div className="logo-text text-primary font-bold text-xl">TaraNaPSU</div>
                    </div>
                    <div className="nav-links flex gap-8">
                        <a href="#features">Features</a>
                        <a href="#about">About</a>
                        <NavLink to="/login" className="text-primary font-bold">Admin Login</NavLink>
                    </div>
                    <div className="flex gap-4">
                        <a href="#download" className="btn btn-primary">
                            <Download size={18} /> Download App
                        </a>
                    </div>
                </div>
            </nav>

            <header className="hero-section">
                <div className="container grid grid-cols-2 items-center gap-8">
                    <div className="hero-content animate-fade-in">
                        <span className="tag text-accent">Official Release v1.0</span>
                        <h1 className="hero-title">
                            Navigate PSU Alaminos <span className="text-primary">With Ease</span>
                        </h1>
                        <p className="hero-description">
                            The official AR navigation companion for Pangasinan State University Alaminos City Campus. Find your classrooms, offices, and facilities effortlessly.
                        </p>
                        <div className="hero-actions flex gap-4">
                            <a href="#download" className="btn btn-primary">
                                Download APK <Download size={18} />
                            </a>
                            <NavLink to="/login" className="btn btn-outline">
                                Admin Dashboard
                            </NavLink>
                        </div>

                        <div className="hero-stats flex gap-8">
                            <div className="stat-item">
                                <span className="stat-value">500+</span>
                                <span className="stat-label">Downloads</span>
                            </div>
                            <div className="stat-item">
                                <span className="stat-value">4.8</span>
                                <span className="stat-label">Rating</span>
                            </div>
                        </div>
                    </div>

                    <div className="hero-image-container flex justify-center">
                        {/* Phone Mockup Frame */}
                        <div className="phone-mockup float-animation">
                            <div className="phone-screen bg-surface flex flex-col items-center justify-center text-center p-4">
                                <div className="mb-4 w-20 h-20 flex items-center justify-center">
                                    <img src={Logo} alt="App Logo" className="w-full h-full object-contain drop-shadow-md" />
                                </div>
                                <h3 className="text-primary font-bold text-lg">TaraNaPSU</h3>
                                <p className="text-xs text-muted mb-6">AR Navigation System</p>
                                <Map size={48} className="text-accent mb-2" />
                                <div className="route-path w-full h-1 bg-gray-100 rounded overflow-hidden mt-4">
                                    <div className="bg-accent h-full w-2/3"></div>
                                </div>
                            </div>
                        </div>
                        {/* Abstract Background Shapes */}
                        <div className="circle-shape c1"></div>
                        <div className="circle-shape c2"></div>
                    </div>
                </div>
            </header>

            <section id="features" className="features-section">
                <div className="container">
                    <div className="section-header text-center">
                        <h2 className="section-title">Smart Campus Navigation</h2>
                        <p className="section-subtitle">Powered by Augmented Reality technology.</p>
                    </div>

                    <div className="grid grid-cols-3 gap-8">
                        <FeatureCard
                            icon={<Navigation size={32} />}
                            title="AR Wayfinding"
                            desc="Visual pathfinding overlaid on the real world through your camera."
                        />
                        <FeatureCard
                            icon={<Map size={32} />}
                            title="Interactive Map"
                            desc="Detailed floor plans of all buildings and facilities."
                        />
                        <FeatureCard
                            icon={<Smartphone size={32} />}
                            title="Offline Capable"
                            desc="Navigate even without an active internet connection."
                        />
                    </div>
                </div>
            </section>

            <section id="download" className="download-section bg-surface py-20">
                <div className="container text-center">
                    <h2 className="section-title mb-8">Get the App Today</h2>
                    <div className="flex justify-center gap-4">
                        <button className="btn btn-primary btn-lg">
                            <Download size={24} /> Download for Android
                        </button>
                    </div>
                    <p className="mt-4 text-muted text-sm">Version 1.0.0 | Android 10+</p>
                </div>
            </section>

            <footer className="footer bg-primary text-white">
                <div className="container text-center">
                    <p>&copy; 2024 TaraNaPSU. Pangasinan State University Alaminos City Campus.</p>
                </div>
            </footer>
        </div>
    );
};

const FeatureCard = ({ icon, title, desc }) => (
    <div className="feature-card card text-center">
        <div className="icon-wrapper">{icon}</div>
        <h3>{title}</h3>
        <p>{desc}</p>
    </div>
);

export default LandingPage;
