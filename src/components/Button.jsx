import styles from '../styles/components/Button.module.css';

function Button(props) {
    return (
        <button 
            className={`${styles.button} ${props.className || ''}`} // Pozwala łączyć style
            onClick={props.onClick}      // 1. Przekazujemy kliknięcie dalej
            type={props.type || "button"} // 2. Pozwala działać jako "submit" w formularzu
        >
            {props.name}
        </button>
    );
}

export default Button;