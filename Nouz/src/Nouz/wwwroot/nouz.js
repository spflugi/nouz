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

    initRightSidebarResize: function (handleElement, sidebarElement, dotNetRef, minWidth, maxWidth) {
        if (!handleElement || !sidebarElement) return;
        if (handleElement._nouzResizeInitialized) return;
        handleElement._nouzResizeInitialized = true;

        let isResizing = false;
        let startX = 0;
        let startWidth = 0;

        const onMouseMove = function (e) {
            if (!isResizing) return;

            // Inverted deltaX for left-edge resizing
            const deltaX = startX - e.clientX;
            let newWidth = startWidth + deltaX;
            newWidth = Math.max(minWidth || 200, Math.min(maxWidth || 600, newWidth));
            sidebarElement.style.width = newWidth + 'px';
        };

        const onMouseUp = async function (e) {
            if (!isResizing) return;

            isResizing = false;
            document.body.classList.remove('right-sidebar-resizing');
            handleElement.classList.remove('resizing');

            const finalWidth = parseInt(sidebarElement.style.width, 10);

            if (dotNetRef && !isNaN(finalWidth)) {
                try {
                    await dotNetRef.invokeMethodAsync('OnRightSidebarResized', finalWidth);
                } catch (err) {
                    console.error('Right sidebar resize error:', err);
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

            document.body.classList.add('right-sidebar-resizing');
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

                // Check if this is a code block - allow Enter to create newlines
                const isCodeBlock = element.closest('.block-code') !== null;

                if (e.key === 'Enter') {
                    if (isCodeBlock) {
                        // For code blocks: Shift+Enter creates new block, Enter creates newline
                        if (e.shiftKey) {
                            e.preventDefault();
                            e.stopPropagation();
                            const content = element.innerText || '';
                            try {
                                await ref.invokeMethodAsync('OnEnterKeyPressed', content);
                            } catch (err) {
                                console.error('Enter key error:', err);
                            }
                        }
                        // Regular Enter in code block - let browser handle newline
                    } else {
                        // For other blocks: Enter creates new block
                        if (!e.shiftKey) {
                            e.preventDefault();
                            e.stopPropagation();
                            const content = element.innerText || '';
                            try {
                                await ref.invokeMethodAsync('OnEnterKeyPressed', content);
                            } catch (err) {
                                console.error('Enter key error:', err);
                            }
                        }
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
    },

    // Get the current selection range within a contenteditable element
    // Returns { start, end, text } or null if no selection
    getSelectionRange: function (element) {
        if (!element) return null;

        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) return null;

        const range = selection.getRangeAt(0);

        // Check if selection is within the element
        if (!element.contains(range.commonAncestorContainer)) return null;

        // Calculate the text offset from the start of the element
        const preSelectionRange = range.cloneRange();
        preSelectionRange.selectNodeContents(element);
        preSelectionRange.setEnd(range.startContainer, range.startOffset);
        const start = preSelectionRange.toString().length;

        const end = start + range.toString().length;
        const text = range.toString();

        return { start, end, text };
    },

    // Set selection range in a contenteditable element
    setSelectionRange: function (element, start, end) {
        if (!element) return;

        const range = document.createRange();
        const selection = window.getSelection();

        let charIndex = 0;
        let startNode = null, startOffset = 0;
        let endNode = null, endOffset = 0;

        const traverseNodes = function (node) {
            if (node.nodeType === Node.TEXT_NODE) {
                const nodeLength = node.textContent.length;
                if (!startNode && charIndex + nodeLength >= start) {
                    startNode = node;
                    startOffset = start - charIndex;
                }
                if (!endNode && charIndex + nodeLength >= end) {
                    endNode = node;
                    endOffset = end - charIndex;
                }
                charIndex += nodeLength;
            } else {
                for (let i = 0; i < node.childNodes.length && !endNode; i++) {
                    traverseNodes(node.childNodes[i]);
                }
            }
        };

        traverseNodes(element);

        if (startNode && endNode) {
            range.setStart(startNode, startOffset);
            range.setEnd(endNode, endOffset);
            selection.removeAllRanges();
            selection.addRange(range);
        }
    },

    // Get the bounding rect of the current selection (for positioning toolbar)
    getSelectionRect: function () {
        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
            return null;
        }

        const range = selection.getRangeAt(0);
        const rect = range.getBoundingClientRect();

        return {
            top: rect.top,
            left: rect.left,
            bottom: rect.bottom,
            right: rect.right,
            width: rect.width,
            height: rect.height
        };
    },

    // Initialize selection monitoring for formatting toolbar
    initFormattingToolbar: function (element, dotNetRef) {
        if (!element || !dotNetRef) return;
        if (element._formattingToolbarInit) {
            element._formattingDotNetRef = dotNetRef;
            return;
        }

        element._formattingToolbarInit = true;
        element._formattingDotNetRef = dotNetRef;

        // Debounce selection change handler
        let selectionTimeout = null;

        const handleSelectionChange = function () {
            if (selectionTimeout) {
                clearTimeout(selectionTimeout);
            }

            selectionTimeout = setTimeout(async function () {
                const ref = element._formattingDotNetRef;
                if (!ref) return;

                const selection = window.getSelection();
                if (!selection || selection.rangeCount === 0) {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', null);
                    } catch (err) { }
                    return;
                }

                const range = selection.getRangeAt(0);

                // Check if selection is within our element
                if (!element.contains(range.commonAncestorContainer)) {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', null);
                    } catch (err) { }
                    return;
                }

                // Has actual selection (not just cursor)
                if (selection.isCollapsed) {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', null);
                    } catch (err) { }
                    return;
                }

                const selectionData = window.nouz.getSelectionRange(element);
                const rect = window.nouz.getSelectionRect();

                if (selectionData && rect && selectionData.text.length > 0) {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', {
                            start: selectionData.start,
                            end: selectionData.end,
                            text: selectionData.text,
                            rect: rect
                        });
                    } catch (err) {
                        console.error('Selection change error:', err);
                    }
                } else {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', null);
                    } catch (err) { }
                }
            }, 100);
        };

        // Listen for selection changes when focused
        element.addEventListener('mouseup', handleSelectionChange);
        element.addEventListener('keyup', function (e) {
            if (e.shiftKey || e.key === 'Shift') {
                handleSelectionChange();
            }
        });

        // Clear selection state when element loses focus
        element.addEventListener('blur', function () {
            // Small delay to allow toolbar click to register
            setTimeout(async function () {
                const ref = element._formattingDotNetRef;
                if (ref && !element.contains(document.activeElement)) {
                    try {
                        await ref.invokeMethodAsync('OnSelectionChanged', null);
                    } catch (err) { }
                }
            }, 200);
        });
    },

    // Set HTML content for a contenteditable element (for formatted content)
    setElementHtml: function (element, html) {
        if (!element) return;
        element.innerHTML = html || '';
    },

    // Get plain text from element (strips HTML)
    getElementPlainText: function (element) {
        if (!element) return '';
        return element.innerText || '';
    }
};