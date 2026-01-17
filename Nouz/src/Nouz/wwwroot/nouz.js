window.nouz = {
    initSidebarResize: function (handleElement, sidebarElement, dotNetRef, minWidth, maxWidth) {
        if (!handleElement || !sidebarElement) return;
        if (handleElement._nouzResizeInitialized) return;
        handleElement._nouzResizeInitialized = true;

        let isResizing = false;
        let startX = 0;
        let startWidth = 0;

        const onMouseMove = function (e) {
            if (!isResizing) return;

            const deltaX = e.clientX - startX;
            let newWidth = startWidth + deltaX;
            newWidth = Math.max(minWidth || 180, Math.min(maxWidth || 400, newWidth));
            sidebarElement.style.width = newWidth + 'px';
        };

        const onMouseUp = async function (e) {
            if (!isResizing) return;

            isResizing = false;
            document.body.classList.remove('sidebar-resizing');
            handleElement.classList.remove('resizing');

            const finalWidth = parseInt(sidebarElement.style.width, 10);

            if (dotNetRef && !isNaN(finalWidth)) {
                try {
                    await dotNetRef.invokeMethodAsync('OnSidebarResized', finalWidth);
                } catch (err) {
                    console.error('Resize error:', err);
                }
            }

            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };

        handleElement.addEventListener('mousedown', function (e) {
            e.preventDefault();
            isResizing = true;
            startX = e.clientX;
            startWidth = sidebarElement.offsetWidth;

            document.body.classList.add('sidebar-resizing');
            handleElement.classList.add('resizing');

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
    },

    focusElement: function (element) {
        if (!element) return;
        element.focus();

        // Move cursor to end of content (for contenteditable elements)
        if (element.isContentEditable) {
            const range = document.createRange();
            const selection = window.getSelection();
            range.selectNodeContents(element);
            range.collapse(false);
            selection.removeAllRanges();
            selection.addRange(range);
        }
    },

    focusInput: function (element) {
        if (!element) return;
        // Use setTimeout to ensure the element is fully rendered in MAUI WebView
        setTimeout(function () {
            element.focus();
            element.select();
        }, 50);
    },

    getElementText: function (element) {
        if (!element) return '';
        return element.innerText || '';
    },

    setElementText: function (element, text) {
        if (!element) return;
        element.innerText = text || '';
    },

    preventDefaultOnEnter: function (element) {
        if (!element) return;
        element.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
            }
            if (e.key === 'Tab') {
                e.preventDefault();
            }
        });
    },

    initBlockEditor: function (element, dotNetRef, initialContent) {
        if (!element) return;

        // Store dotNetRef first so it's available for event listeners
        element._dotNetRef = dotNetRef;

        // Set initial content and attach listeners only on first init
        if (!element._blockEditorInit) {
            element._blockEditorInit = true;

            // Set initial content
            if (initialContent !== undefined && initialContent !== null) {
                element.innerText = initialContent;
            }

            element.addEventListener('keydown', async function (e) {
                // Use stored dotNetRef which may be updated on re-renders
                const ref = element._dotNetRef;
                if (!ref) return;

                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    e.stopPropagation();
                    const content = element.innerText || '';
                    try {
                        await ref.invokeMethodAsync('OnEnterKeyPressed', content);
                    } catch (err) {
                        console.error('Enter key error:', err);
                    }
                }
                if (e.key === 'Tab') {
                    e.preventDefault();
                    e.stopPropagation();
                    try {
                        await ref.invokeMethodAsync('OnTabKeyPressed', e.shiftKey);
                    } catch (err) {
                        console.error('Tab key error:', err);
                    }
                }
            });
        }
    }
};