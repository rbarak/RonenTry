'use strict';

const API_URL = (window.APP_CONFIG && window.APP_CONFIG.apiUrl) || 'http://localhost:8080/api/offices/register';

const form = document.getElementById('registration-form');
const submitBtn = document.getElementById('submit-btn');
const spinner = document.getElementById('spinner');
const messageBanner = document.getElementById('message');

const FIELD_RULES = {
    officeName: { required: true },
    managerName: { required: true },
    email: {
        required: true,
        regex: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    },
    phone: {
        required: true,
        regex: /^\d{10,15}$/
    },
    userName: { required: true },
    password: {
        required: true,
        regex: /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/
    }
};

function getInput(id) {
    return document.getElementById(id);
}

function getError(id) {
    return document.getElementById('error-' + id);
}

function setFieldValidity(fieldId, isValid) {
    const input = getInput(fieldId);
    const error = getError(fieldId);
    if (input) input.classList.toggle('invalid', !isValid);
    if (error) error.classList.toggle('hidden', isValid);
}

function validateForm() {
    let valid = true;

    for (const [fieldId, rules] of Object.entries(FIELD_RULES)) {
        const input = getInput(fieldId);
        if (!input) continue;

        const value = fieldId === 'password' ? input.value : input.value.trim();

        if (rules.required && !value) {
            setFieldValidity(fieldId, false);
            valid = false;
            continue;
        }

        if (rules.regex && value && !rules.regex.test(value)) {
            setFieldValidity(fieldId, false);
            valid = false;
            continue;
        }

        setFieldValidity(fieldId, true);
    }

    return valid;
}

function showBanner(text, type) {
    messageBanner.textContent = text;
    messageBanner.className = 'message ' + type;
}

function hideBanner() {
    messageBanner.className = 'message hidden';
}

function setLoading(loading) {
    submitBtn.disabled = loading;
    spinner.classList.toggle('hidden', !loading);
}

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideBanner();

    if (!validateForm()) return;

    setLoading(true);

    const payload = {
        officeName: getInput('officeName').value.trim(),
        managerName: getInput('managerName').value.trim(),
        email: getInput('email').value.trim(),
        phone: getInput('phone').value.trim(),
        userName: getInput('userName').value.trim(),
        password: getInput('password').value
    };

    try {
        const response = await fetch(API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await response.json();

        if (data.success) {
            showBanner('המשרד נרשם בהצלחה! מספר משרד: ' + data.officeId, 'success');
            form.reset();
            Object.keys(FIELD_RULES).forEach(id => setFieldValidity(id, true));
        } else {
            showBanner(data.message || 'אירעה שגיאה. נסה שנית.', 'error');
        }
    } catch {
        showBanner('שגיאת חיבור לשרת. נסה שנית מאוחר יותר.', 'error');
    } finally {
        setLoading(false);
    }
});
