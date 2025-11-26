import styles from '../styles/components/modal.module.css';

function Modal({ onClose, children }) {
    // Funkcja obsługująca kliknięcie w tło - zamyka modal
    const handleBackdropClick = (e) => {
        // Sprawdzamy, czy kliknięto bezpośrednio w tło (a nie w zawartość)
        if (e.target === e.currentTarget) {
            onClose();
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={handleBackdropClick}>
            <div className={styles.modalContent}>
                <button className={styles.closeButton} onClick={onClose}>
                    &times;
                </button>
                {children}
            </div>

        </div>
    );
}

export default Modal;