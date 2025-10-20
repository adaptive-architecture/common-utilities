// Version 1.2.3
console.log('App.js loaded - Version 1.2.3');

function updateVersionInfo() {
    const versionElement = document.getElementById('js-version');
    if (versionElement) {
        versionElement.textContent = 'v1.2.3 (Current)';
        versionElement.style.color = '#28a745';
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', updateVersionInfo);
} else {
    updateVersionInfo();
}
