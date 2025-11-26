import React, { useState } from 'react';
import styles from '../styles/pages/auth.module.css';
import { Link } from 'react-router-dom';

const SignUp = () => {
    const [formData, setFormData] = useState({
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
    });

    const handleChange = (e) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        if (formData.password !== formData.confirmPassword) {
            alert("Passwords do not match!");
            return;
        }
        // Logika rejestracji w backendzie C#
        console.log("Rejestracja:", formData);
    };

    return (
        <div className={styles.pageContainer}>
            <div className={styles.authCard}>
                <h2 className={styles.title}>Create Account</h2>
                <p className={styles.subtitle}>Join the eSports community today</p>

                <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                    
                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Username</label>
                        <input 
                            type="text" 
                            name="username"
                            className={styles.input} 
                            placeholder="GamerTag123"
                            value={formData.username}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Email Address</label>
                        <input 
                            type="email" 
                            name="email"
                            className={styles.input} 
                            placeholder="name@example.com"
                            value={formData.email}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Password</label>
                        <input 
                            type="password" 
                            name="password"
                            className={styles.input} 
                            placeholder="••••••••"
                            value={formData.password}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Confirm Password</label>
                        <input 
                            type="password" 
                            name="confirmPassword"
                            className={styles.input} 
                            placeholder="••••••••"
                            value={formData.confirmPassword}
                            onChange={handleChange}
                            required
                        />
                    </div>

                    <button type="submit" className={styles.submitButton}>
                        Sign Up
                    </button>
                </form>

                <p className={styles.footerText}>
                    Already have an account? 
                    {/* Jeśli używasz React Router, zamień <a> na <Link to="/login"> */}
                    <Link to="/login" className={styles.link}>Log in</Link>
                </p>
            </div>
        </div>
    );
};

export default SignUp;