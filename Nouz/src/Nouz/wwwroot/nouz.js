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
    }
};