// Message Composer JavaScript functionality
class MessageComposer {
    constructor() {
        this.textarea = null;
        this.isInitialized = false;
    }

    initialize(textareaElement) {
        this.textarea = textareaElement;
        this.isInitialized = true;
        this.setupEventListeners();
        this.adjustHeight();
    }

    setupEventListeners() {
        if (!this.textarea) return;

        // Auto-resize textarea
        this.textarea.addEventListener('input', () => {
            this.adjustHeight();
        });

        // Handle paste events for files
        this.textarea.addEventListener('paste', (e) => {
            this.handlePaste(e);
        });

        // Handle drag and drop
        this.textarea.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        this.textarea.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.handleFileDrop(e);
        });
    }

    adjustHeight() {
        if (!this.textarea) return;

        // Reset height to auto to get the correct scrollHeight
        this.textarea.style.height = 'auto';
        
        // Calculate new height
        const minHeight = 40;
        const maxHeight = 200;
        const scrollHeight = this.textarea.scrollHeight;
        const newHeight = Math.min(Math.max(scrollHeight, minHeight), maxHeight);
        
        // Set the new height
        this.textarea.style.height = newHeight + 'px';
        
        // Show scrollbar if content exceeds max height
        this.textarea.style.overflowY = scrollHeight > maxHeight ? 'auto' : 'hidden';
    }

    focus() {
        if (this.textarea) {
            this.textarea.focus();
            // Move cursor to end
            this.textarea.setSelectionRange(this.textarea.value.length, this.textarea.value.length);
        }
    }

    applyFormatting(prefix, suffix) {
        if (!this.textarea) return;

        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const selectedText = this.textarea.value.substring(start, end);
        const beforeText = this.textarea.value.substring(0, start);
        const afterText = this.textarea.value.substring(end);

        let newText, newCursorPos;

        if (selectedText) {
            // If text is selected, wrap it with formatting
            newText = beforeText + prefix + selectedText + suffix + afterText;
            newCursorPos = start + prefix.length + selectedText.length + suffix.length;
        } else {
            // If no text selected, insert formatting markers and position cursor between them
            newText = beforeText + prefix + suffix + afterText;
            newCursorPos = start + prefix.length;
        }

        // Update textarea value
        this.textarea.value = newText;
        
        // Trigger input event to update Blazor component
        this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
        
        // Set cursor position
        this.textarea.focus();
        this.textarea.setSelectionRange(newCursorPos, newCursorPos);
        
        this.adjustHeight();
    }

    insertText(text) {
        if (!this.textarea) return;

        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const beforeText = this.textarea.value.substring(0, start);
        const afterText = this.textarea.value.substring(end);

        this.textarea.value = beforeText + text + afterText;
        
        // Trigger input event
        this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
        
        // Set cursor position after inserted text
        const newCursorPos = start + text.length;
        this.textarea.focus();
        this.textarea.setSelectionRange(newCursorPos, newCursorPos);
        
        this.adjustHeight();
    }

    handlePaste(event) {
        const items = event.clipboardData?.items;
        if (!items) return;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            
            // Handle pasted images
            if (item.type.indexOf('image') !== -1) {
                event.preventDefault();
                const blob = item.getAsFile();
                if (blob) {
                    this.handleImagePaste(blob);
                }
                return;
            }
        }
    }

    handleImagePaste(blob) {
        // Create a file from the blob
        const file = new File([blob], `pasted-image-${Date.now()}.png`, {
            type: blob.type
        });

        // Trigger file upload through Blazor
        this.triggerFileUpload([file]);
    }

    handleFileDrop(event) {
        const files = Array.from(event.dataTransfer.files);
        if (files.length > 0) {
            this.triggerFileUpload(files);
        }
    }

    triggerFileUpload(files) {
        // This would integrate with Blazor's file upload component
        // For now, we'll dispatch a custom event
        const event = new CustomEvent('filesDropped', {
            detail: { files: files }
        });
        this.textarea.dispatchEvent(event);
    }

    getMentionPosition() {
        if (!this.textarea) return { top: '0px', left: '0px' };

        const textareaRect = this.textarea.getBoundingClientRect();
        const textBeforeCursor = this.textarea.value.substring(0, this.textarea.selectionStart);
        
        // Create a temporary element to measure text position
        const tempDiv = document.createElement('div');
        tempDiv.style.cssText = `
            position: absolute;
            visibility: hidden;
            white-space: pre-wrap;
            word-wrap: break-word;
            font-family: ${getComputedStyle(this.textarea).fontFamily};
            font-size: ${getComputedStyle(this.textarea).fontSize};
            line-height: ${getComputedStyle(this.textarea).lineHeight};
            padding: ${getComputedStyle(this.textarea).padding};
            border: ${getComputedStyle(this.textarea).border};
            width: ${this.textarea.offsetWidth}px;
        `;
        
        tempDiv.textContent = textBeforeCursor;
        document.body.appendChild(tempDiv);
        
        const tempRect = tempDiv.getBoundingClientRect();
        document.body.removeChild(tempDiv);
        
        return {
            top: (textareaRect.top + tempRect.height - this.textarea.scrollTop + 20) + 'px',
            left: textareaRect.left + 'px'
        };
    }

    // Utility functions for text manipulation
    getSelectedText() {
        if (!this.textarea) return '';
        return this.textarea.value.substring(this.textarea.selectionStart, this.textarea.selectionEnd);
    }

    getCursorPosition() {
        return this.textarea ? this.textarea.selectionStart : 0;
    }

    setCursorPosition(position) {
        if (this.textarea) {
            this.textarea.setSelectionRange(position, position);
        }
    }

    getTextBeforeCursor() {
        if (!this.textarea) return '';
        return this.textarea.value.substring(0, this.textarea.selectionStart);
    }

    getTextAfterCursor() {
        if (!this.textarea) return '';
        return this.textarea.value.substring(this.textarea.selectionEnd);
    }

    // Markdown helpers
    insertBold() {
        this.applyFormatting('**', '**');
    }

    insertItalic() {
        this.applyFormatting('_', '_');
    }

    insertStrikethrough() {
        this.applyFormatting('~~', '~~');
    }

    insertCode() {
        this.applyFormatting('`', '`');
    }

    insertCodeBlock() {
        this.applyFormatting('\n```\n', '\n```\n');
    }

    insertBulletList() {
        const lines = this.getSelectedText().split('\n');
        const formattedLines = lines.map(line => line.trim() ? `- ${line.trim()}` : line);
        this.replaceSelection(formattedLines.join('\n'));
    }

    insertNumberedList() {
        const lines = this.getSelectedText().split('\n');
        const formattedLines = lines.map((line, index) => 
            line.trim() ? `${index + 1}. ${line.trim()}` : line
        );
        this.replaceSelection(formattedLines.join('\n'));
    }

    insertQuote() {
        const lines = this.getSelectedText().split('\n');
        const formattedLines = lines.map(line => line.trim() ? `> ${line.trim()}` : line);
        this.replaceSelection(formattedLines.join('\n'));
    }

    replaceSelection(newText) {
        if (!this.textarea) return;

        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const beforeText = this.textarea.value.substring(0, start);
        const afterText = this.textarea.value.substring(end);

        this.textarea.value = beforeText + newText + afterText;
        this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
        
        this.textarea.focus();
        this.textarea.setSelectionRange(start + newText.length, start + newText.length);
        this.adjustHeight();
    }

    // Cleanup
    destroy() {
        this.textarea = null;
        this.isInitialized = false;
    }
}

// Global instance
const messageComposer = new MessageComposer();

// Export for Blazor interop
window.messageComposer = {
    initialize: (textarea) => messageComposer.initialize(textarea),
    focus: () => messageComposer.focus(),
    adjustHeight: () => messageComposer.adjustHeight(),
    applyFormatting: (textarea, prefix, suffix) => {
        if (!messageComposer.isInitialized) {
            messageComposer.initialize(textarea);
        }
        messageComposer.applyFormatting(prefix, suffix);
    },
    insertText: (text) => messageComposer.insertText(text),
    getMentionPosition: () => messageComposer.getMentionPosition(),
    destroy: () => messageComposer.destroy()
};

// Auto-export for ES6 modules
export const {
    initialize,
    focus,
    adjustHeight,
    applyFormatting,
    insertText,
    getMentionPosition,
    destroy
} = window.messageComposer;