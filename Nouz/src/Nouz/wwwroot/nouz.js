window.nouz = {
    setTheme: function (isDark) {
        if (isDark) {
            document.documentElement.classList.add('dark-mode');
        } else {
            document.documentElement.classList.remove('dark-mode');
        }
    },


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
                if (e.key === 's' && (e.ctrlKey || e.metaKey)) {
                    e.preventDefault();
                    e.stopPropagation();
                    try {
                        await ref.invokeMethodAsync('OnSaveKeyPressed');
                    } catch (err) {
                        console.error('Save key error:', err);
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
    },

    // Scroll an element to the bottom (for chat)
    scrollToBottom: function (element) {
        if (!element) return;
        element.scrollTop = element.scrollHeight;
    },

    // Initialize image paste handling for a block
    initImagePasteHandler: function (element, dotNetRef) {
        if (!element || !dotNetRef) return;
        if (element._imagePasteInit) {
            element._imagePasteDotNetRef = dotNetRef;
            return;
        }

        element._imagePasteInit = true;
        element._imagePasteDotNetRef = dotNetRef;

        element.addEventListener('paste', async function (e) {
            const ref = element._imagePasteDotNetRef;
            if (!ref) return;

            const items = e.clipboardData?.items;
            if (!items) return;

            for (let i = 0; i < items.length; i++) {
                const item = items[i];
                if (item.type.startsWith('image/')) {
                    e.preventDefault();
                    e.stopPropagation();

                    const file = item.getAsFile();
                    if (file) {
                        const base64 = await window.nouz.fileToBase64(file);
                        const fileName = file.name || 'pasted-image.' + item.type.split('/')[1];
                        try {
                            await ref.invokeMethodAsync('OnImagePastedFromClipboard', base64, fileName, item.type);
                        } catch (err) {
                            console.error('Image paste error:', err);
                        }
                    }
                    break;
                }
            }
        });

        // Handle drag and drop for images
        element.addEventListener('dragover', function (e) {
            if (e.dataTransfer?.types?.includes('Files')) {
                e.preventDefault();
                e.stopPropagation();
                element.classList.add('drag-over-image');
            }
        });

        element.addEventListener('dragleave', function (e) {
            element.classList.remove('drag-over-image');
        });

        element.addEventListener('drop', async function (e) {
            element.classList.remove('drag-over-image');
            const ref = element._imagePasteDotNetRef;
            if (!ref) return;

            const files = e.dataTransfer?.files;
            if (!files || files.length === 0) return;

            for (let i = 0; i < files.length; i++) {
                const file = files[i];
                if (file.type.startsWith('image/')) {
                    e.preventDefault();
                    e.stopPropagation();

                    const base64 = await window.nouz.fileToBase64(file);
                    try {
                        await ref.invokeMethodAsync('OnImagePastedFromClipboard', base64, file.name, file.type);
                    } catch (err) {
                        console.error('Image drop error:', err);
                    }
                    break;
                }
            }
        });
    },

    // Convert a file to base64 string
    fileToBase64: function (file) {
        return new Promise(function (resolve, reject) {
            const reader = new FileReader();
            reader.onload = function () {
                // Remove the data URL prefix to get just the base64 string
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    },

    // Trigger file input click for image selection
    triggerImageFileInput: function (inputElement) {
        if (inputElement) {
            inputElement.click();
        }
    },

    // Process image file input and invoke C# method
    processImageFileInput: async function (inputElement, dotNetRef) {
        if (!inputElement || !dotNetRef) return;

        const files = inputElement.files;
        if (!files || files.length === 0) return;

        const file = files[0];
        if (!file.type.startsWith('image/')) return;

        try {
            const base64 = await window.nouz.fileToBase64(file);
            await dotNetRef.invokeMethodAsync('OnImagePastedFromClipboard', base64, file.name, file.type);
        } catch (err) {
            console.error('Image file processing error:', err);
        }

        // Clear the input so the same file can be selected again
        inputElement.value = '';
    },

    // Initialize image resize handler on element
    initImageResizeHandler: function (resizeHandle, blockElement, dotNetRef) {
        if (!resizeHandle || !blockElement || !dotNetRef) return;
        if (resizeHandle._resizeInitialized) return;
        resizeHandle._resizeInitialized = true;

        resizeHandle.addEventListener('mousedown', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const container = blockElement.querySelector('.image-resizable-container');
            if (!container) return;

            const parentWidth = blockElement.offsetWidth;
            const startX = e.clientX;
            const startWidth = container.offsetWidth;

            document.body.classList.add('image-resizing');

            const onMouseMove = function (moveEvent) {
                const deltaX = moveEvent.clientX - startX;
                let newWidth = startWidth + deltaX;

                // Calculate percentage based on parent width
                let newPercent = Math.round((newWidth / parentWidth) * 100);
                newPercent = Math.max(10, Math.min(100, newPercent));

                container.style.width = newPercent + '%';

                // Update caption width too
                const caption = blockElement.querySelector('.image-caption');
                if (caption) {
                    caption.style.width = newPercent + '%';
                }
            };

            const onMouseUp = async function () {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
                document.body.classList.remove('image-resizing');

                // Get final percentage
                const finalWidth = container.offsetWidth;
                const finalPercent = Math.round((finalWidth / parentWidth) * 100);

                try {
                    await dotNetRef.invokeMethodAsync('OnImageResized', finalPercent);
                } catch (err) {
                    console.error('Image resize error:', err);
                }
            };

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
    },

    // Trigger a file input element click
    triggerFileInput: function (inputElement) {
        if (inputElement) {
            inputElement.click();
        }
    },

    // Download a file with the given content
    downloadFile: function (content, filename, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },

    // Open print dialog for HTML content (for PDF export)
    printHtml: function (htmlContent, title) {
        // Use iframe approach which works better in WebView environments
        var printFrame = document.getElementById('nouz-print-frame');

        // Create hidden iframe if it doesn't exist
        if (!printFrame) {
            printFrame = document.createElement('iframe');
            printFrame.id = 'nouz-print-frame';
            printFrame.style.position = 'fixed';
            printFrame.style.right = '0';
            printFrame.style.bottom = '0';
            printFrame.style.width = '0';
            printFrame.style.height = '0';
            printFrame.style.border = 'none';
            document.body.appendChild(printFrame);
        }

        // Write content to iframe and print
        var frameDoc = printFrame.contentWindow || printFrame.contentDocument;
        if (frameDoc.document) {
            frameDoc = frameDoc.document;
        }

        frameDoc.open();
        frameDoc.write(htmlContent);
        frameDoc.close();

        // Wait for content to render, then print
        setTimeout(function () {
            printFrame.contentWindow.focus();
            printFrame.contentWindow.print();
        }, 300);
    }
};