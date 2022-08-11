-- Connection user must have the following system priviliges:
-- 1. Create any tables
-- 2. Create any sequences
-- 3. Create any procedures
-- 4. Optional. In case it is expected that data notification will be used (in lieu of polling),
--    connection user must be granted with change notification privilege: GRANT CHANGE NOTIFICATION TO &DBOWNER
DECLARE
 lCount NUMBER;
BEGIN
SELECT COUNT(1) INTO lCount FROM USER_TABLES WHERE TABLE_NAME = 'MESSAGES';

IF lCount = 0 THEN
EXECUTE IMMEDIATE
'CREATE TABLE MESSAGES(
    PayloadId	NUMBER(11,0) NOT NULL,
    Payload		BLOB		 NOT NULL,
    InsertedOn	DATE         NOT NULL,
    CONSTRAINT  PK_PAYLOAD_ID PRIMARY KEY(PayloadId)
) TABLESPACE &TBLSPC_TDS_TABLES';
END IF;

SELECT COUNT(1) INTO lCount FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'SEQ_MESSAGES';

IF lCount = 0 THEN
EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_MESSAGES MAXVALUE 99999999999 CYCLE START WITH 1 INCREMENT BY 1 NOCACHE';
END IF;

SELECT COUNT(1) INTO lCount FROM user_objects WHERE OBJECT_TYPE = 'PACKAGE' AND OBJECT_NAME = 'SIGNALR';
IF lCount = 0 THEN
EXECUTE IMMEDIATE
'CREATE PACKAGE SIGNALR AS 
PROCEDURE PAYLOAD_INSERT(
    iPayload	IN BLOB
);
PROCEDURE PAYLOADID_GET(
    oPayloadId  OUT MESSAGES.PayloadId%TYPE
);
PROCEDURE PAYLOAD_READ(
    iPayloadId	IN  MESSAGES.PayloadId%TYPE,
    oRefCur     OUT SYS_REFCURSOR
);
END SIGNALR;';

EXECUTE IMMEDIATE
'CREATE PACKAGE BODY SIGNALR AS

PROCEDURE PAYLOAD_INSERT(
iPayload	IN BLOB
) AS
    lNewPayloadId MESSAGES.PayloadId%TYPE;
    MaxTableSize CONSTANT NUMBER := 10000;
    BlockSize    CONSTANT NUMBER := 2500;
    lRowCount     NUMBER;
    lStartPayloadId MESSAGES.PayloadId%TYPE;
    lEndPayloadId   MESSAGES.PayloadId%TYPE; 
    lOverMaxBy NUMBER;
BEGIN
SELECT SEQ_MESSAGES.NEXTVAL INTO lNewPayloadId FROM DUAL;
INSERT INTO MESSAGES(PayloadId, Payload,InsertedOn) VALUES(lNewPayloadId, iPayload, SYS_EXTRACT_UTC(SYSTIMESTAMP));

COMMIT;
-- Garbage collection
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
  IF lNewPayloadId MOD BlockSize = 0 THEN
    SELECT COUNT(PayloadId), MIN(PayloadId) INTO lRowCount, lStartPayloadId FROM MESSAGES;
    
    -- Check if we are over the max table size
    IF lRowCount >= MaxTableSize THEN
      
      -- We want to delete enough rows to bring the table back to max size - block size
      lOverMaxBy := lRowCount - MaxTableSize;
      lEndPayloadId := lStartPayloadId + BlockSize + lOverMaxBy;
      
      -- Delete oldest block of messages
      DELETE FROM MESSAGES WHERE PayloadId BETWEEN lStartPayloadId AND lEndPayloadId;
    END IF;
   COMMIT;
  END IF;

END PAYLOAD_INSERT;

PROCEDURE PAYLOADID_GET(
    oPayloadId  OUT MESSAGES.PayloadId%TYPE
) AS
lPayloadId MESSAGES.PayloadId%TYPE;
BEGIN
SELECT LAST_NUMBER - 1 INTO oPayloadId FROM USER_SEQUENCES WHERE SEQUENCE_NAME = ''SEQ_MESSAGES'';
END PAYLOADID_GET;

PROCEDURE PAYLOAD_READ(
    iPayloadId	IN  MESSAGES.PayloadId%TYPE,
    oRefCur     OUT SYS_REFCURSOR	
) AS
BEGIN
OPEN oRefCur FOR
SELECT PayloadId, Payload, InsertedOn FROM MESSAGES
WHERE PayloadId > iPayloadId
ORDER BY PayloadId ASC;
END PAYLOAD_READ;
END SIGNALR;';
END IF;

END;