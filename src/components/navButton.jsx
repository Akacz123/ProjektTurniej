import styles from '../styles/components/NavButton.module.css';

function NavButton(props) {
  return (
      <div className={styles.button}>
          <img src="rocket.svg" className={styles.icon} alt='icon' />
          <span>{props.name}</span>
      </div>
    // <button className={styles.button}>{props.name}</button>
  );
}

export default NavButton;