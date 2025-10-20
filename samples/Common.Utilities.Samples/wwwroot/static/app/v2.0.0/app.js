// Version 2.0.0
console.log('App.js loaded - Version 2.0.0 (Beta)');

function updateVersionInfo() {
    const versionElement = document.getElementById('js-version');
    if (versionElement) {
        versionElement.textContent = 'v2.0.0 (Beta)';
        versionElement.style.color = '#ff6b6b';
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', updateVersionInfo);
} else {
    updateVersionInfo();
}
