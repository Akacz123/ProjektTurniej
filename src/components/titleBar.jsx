import styles from "../styles/components/TitleBar.module.css";
import Button from "./Button";
import logo from "../assets/logo.png";

function TitleBar() {

    const isLoggedIn = false;
    const user = "guest";
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
                        <Button name="log in" className={styles.logInButton} />
                        <Button name="sign up" className={styles.signUpButton} />
                    </div>}

            </div>
        </header>
    );
}

export default TitleBar;