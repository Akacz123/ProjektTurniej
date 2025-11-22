import styles from '../styles/components/Button.module.css';

function Button(props) {
    return (
        <button className={styles.button}>{props.name}</button>
    );
}

export default Button;