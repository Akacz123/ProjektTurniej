import React, { useState } from 'react';
import styles from '../styles/pages/auth.module.css';
import { Link } from 'react-router-dom';
import { useNavigate } from 'react-router-dom';

const LogIn = () => {
    const navigate = useNavigate();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const handleSubmit = (e) => {
        e.preventDefault();
        // Tutaj będzie logika łączenia z backendem C#
        console.log("Logowanie:", email, password);
    };

    return (
        <div className={styles.pageContainer}>
            <div className={styles.authCard}>
                <h2 className={styles.title}>Welcome Back</h2>
                <p className={styles.subtitle}>Log in to manage your tournaments</p>

                <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                    
                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Email Address</label>
                        <input 
                            type="email" 
                            className={styles.input} 
                            placeholder="name@example.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Password</label>
                        <input 
                            type="password" 
                            className={styles.input} 
                            placeholder="••••••••"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </div>

                    <button type="submit" className={styles.submitButton} onClick={() => navigate('/')}>
                        Log In
                    </button>
                </form>

                <p className={styles.footerText}>
                    Don't have an account? 
                    <Link to="/signup" className={styles.link}>Sign up</Link>
                </p>
            </div>
        </div>
    );
};

export default LogIn;