window.scrollToElement = function (id) {
    const container = document.getElementById(id);
    if (container) {
        container.lastElementChild.scrollIntoView({ behavior: 'smooth', block: 'end' });
    } else {
        console.error('Container element not found');
    }
}

window.registerViewportChangeCallback = (dotnetHelper) => {
    window.addEventListener('load', () => {
        dotnetHelper.invokeMethodAsync('OnResize', window.innerWidth, window.innerHeight);
    });
    window.addEventListener('resize', () => {
        dotnetHelper.invokeMethodAsync('OnResize', window.innerWidth, window.innerHeight);
    });
};

window.dispatchResizeEvent = function () {
    window.dispatchEvent(new Event('resize'));
};

window.resizeListener = function (dotnethelper) {
    const mainWidth = document.querySelector('main').offsetWidth;
    const mainHeight = document.querySelector('main').offsetHeight;

    const inputWrapper = document.querySelector('.input-wrapper');
    if (inputWrapper) {
        inputWrapper.style.width = `${mainWidth - 100}px`;
    }

    const shortcuts = document.querySelector('.shortcuts-wrapper');
    if (shortcuts) {
        shortcuts.style.width = `${mainWidth - 100}px`;
    }

    window.addEventListener('resize', () => {
        const mainWidth = document.querySelector('main').offsetWidth;
        const mainHeight = document.querySelector('main').offsetHeight;

        dotnethelper.invokeMethodAsync('GetMainWidth', mainWidth, mainHeight)
            .then(() => {
                const inputWrapper = document.querySelector('.input-wrapper');
                if (inputWrapper) {
                    inputWrapper.style.width = `${mainWidth - 100}px`;
                }

                const shortcuts = document.querySelector('.shortcuts-wrapper');
                if (shortcuts) {
                    shortcuts.style.width = `${mainWidth - 100}px`;
                }

                let filters = document.getElementById('filters');
                let filtersHeight = filters ? filters.offsetHeight : 0;

                const chatMessages = document.getElementById('chatMessages');
                if (chatMessages) {
                    const newHeight = mainHeight -
                        (inputWrapper.offsetHeight + shortcuts.offsetHeight + filtersHeight + 100);

                    if (chatMessages.offsetHeight > newHeight) {
                        chatMessages.style.height = `${newHeight}px`;
                    }
                }
            })
            .catch(error => {
                console.log("Error during browser resize: " + error);
            });
    });
};