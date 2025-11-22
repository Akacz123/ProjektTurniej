import styles from '../styles/components/Modal.module.css';

function Modal({ onClose, children }) {
    // Funkcja obsługująca kliknięcie w tło - zamyka modal
    const handleBackdropClick = (e) => {
        // Sprawdzamy, czy kliknięto bezpośrednio w tło (a nie w zawartość)
        if (e.target === e.currentTarget) {
            onClose();
        }
    };

    return (
        // Tło modala (overlay)
        <div className={styles.modalOverlay} onClick={handleBackdropClick}>
            
            {/* Właściwa treść modala */}
            <div className={styles.modalContent}>
                {/* Przycisk do zamykania */}
                <button className={styles.closeButton} onClick={onClose}>
                    &times; {/* To jest znak "X" */}
                </button>
                
                {/* Tutaj renderowana jest zawartość przekazana do komponentu */}
                {children}
            </div>

        </div>
    );
}

export default Modal;