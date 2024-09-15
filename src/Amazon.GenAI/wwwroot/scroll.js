function scrollToElement(id) {
    const container = document.getElementById(id);
    if (container) {
        console.log(container.lastElementChild);
        container.lastElementChild.scrollIntoView({ behavior: 'smooth', block: 'end' });
    } else {
        console.error('Container element not found');
    }
}