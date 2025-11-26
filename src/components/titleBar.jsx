import styles from "../styles/components/titleBar.module.css";
import Button from "./Button";
import logo from "../assets/logo.png";
import { useNavigate } from 'react-router-dom';

function TitleBar() {

    const isLoggedIn = false;
    const user = "guest";
    const navigate = useNavigate();



    return (
        <header className={styles.header}>
            <div className={styles.titleSection}>

                <div className={styles.logo}>
                    <img src={logo} alt="logo" className={styles.logo}/>
                </div>
                <div className={styles.title}>
                    <h2>eSports Tournament organizer</h2>
                </div>
            </div>
            <div className={styles.headerRight}>
                {isLoggedIn ?
                    <span>Welcome, {user}</span> :
                    <div className={styles.authButtons}>
                        <Button name="log in" onClick={() => navigate('/login')}  className={styles.logInButton} />
                        <Button name="sign up" onClick={() => navigate('/signup')} className={styles.signUpButton} />
                    </div>}

            </div>
        </header>
    );
}

export default TitleBar;