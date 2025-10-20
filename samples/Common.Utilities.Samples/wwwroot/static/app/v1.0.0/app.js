// Version 1.0.0
console.log('App.js loaded - Version 1.0.0');

function updateVersionInfo() {
    const versionElement = document.getElementById('js-version');
    if (versionElement) {
        versionElement.textContent = 'v1.0.0 (Default/Fallback)';
        versionElement.style.color = '#666';
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', updateVersionInfo);
} else {
    updateVersionInfo();
}
